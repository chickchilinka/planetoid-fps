using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Modules.Multiplayer.Primitives;

namespace Modules.Multiplayer.Session
{
    public sealed class ServerSessionFacade : IServerSessionFacade
    {
        private readonly SessionConfiguration _configuration;
        private readonly SessionCommandQueue _queue;
        private readonly IMatchWorldProvider _worldProvider;
        private readonly IPlayerSpawnProvider _spawnProvider;
        private readonly ISessionEventPublisher _eventPublisher;
        private readonly ISessionConnectionProvider _connectionProvider;
        private readonly ISessionTelemetry _telemetry;
        private readonly IIdentifierProvider _identifiers;
        private readonly Dictionary<PlayerId, SessionPlayer> _players = new Dictionary<PlayerId, SessionPlayer>();
        private readonly Dictionary<SessionConnection, PlayerId> _playersByConnection =
            new Dictionary<SessionConnection, PlayerId>();
        private readonly HashSet<PlayerId> _initialParticipants = new HashSet<PlayerId>();
        private readonly Dictionary<PlayerId, PlayerTimeoutContext> _playerTimeouts =
            new Dictionary<PlayerId, PlayerTimeoutContext>();
        private readonly HashSet<PlayerId> _terminalDisconnectingPlayers = new HashSet<PlayerId>();
        private readonly Dictionary<PlayerId, CancellationTokenSource> _playerWork =
            new Dictionary<PlayerId, CancellationTokenSource>();
        private readonly object _publicationSync = new object();
        private readonly Queue<PublicationItem> _pendingPublications = new Queue<PublicationItem>();
        private readonly SemaphoreSlim _publicationGate = new SemaphoreSlim(1, 1);
        private readonly AsyncLocal<bool> _isPublishing = new AsyncLocal<bool>();
        private readonly SessionId _sessionId;

        private SessionSnapshot _snapshot;
        private SessionPhase _phase = SessionPhase.WaitingForPlayers;
        private MatchId _matchId = MatchId.None;
        private MapId _mapId = MapId.None;
        private OperationId _operationId = OperationId.None;
        private WorldLoadContext _worldLoad;
        private long _revision;
        private bool _serverWorldReady;

        public ServerSessionFacade(
            SessionConfiguration configuration,
            SessionCommandQueue queue,
            IMatchWorldProvider worldProvider,
            IPlayerSpawnProvider spawnProvider,
            ISessionEventPublisher eventPublisher,
            ISessionConnectionProvider connectionProvider,
            ISessionTelemetry telemetry,
            IIdentifierProvider identifiers)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
            _worldProvider = worldProvider ?? throw new ArgumentNullException(nameof(worldProvider));
            _spawnProvider = spawnProvider ?? throw new ArgumentNullException(nameof(spawnProvider));
            _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
            _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
            _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
            _identifiers = identifiers ?? throw new ArgumentNullException(nameof(identifiers));
            _sessionId = new SessionId(RequireNewGuid("session"));
            _snapshot = BuildSnapshot();
        }

        public SessionSnapshot Snapshot => Volatile.Read(ref _snapshot);

        public async ValueTask<Result<PlayerId, SessionError>> JoinAsync(
            SessionConnection connection,
            CancellationToken token)
        {
            if (!connection.IsValid)
                throw new ArgumentException("Session connection must be valid.", nameof(connection));

            var candidatePlayerId = new PlayerId(RequireNewGuid("player"));
            Task publication = null;
            PlayerTimeoutContext timeout = null;
            Result<PlayerId, SessionError> result = default;
            var rejected = false;
            await _queue.ExecuteAsync(() =>
            {
                if (_playersByConnection.TryGetValue(connection, out var existing))
                {
                    result = Result<PlayerId, SessionError>.Success(existing);
                    return Completed();
                }

                if (_players.Count >= _configuration.MaxPlayers)
                {
                    rejected = true;
                    result = Result<PlayerId, SessionError>.Failure(SessionError.SessionFull);
                    return Completed();
                }

                var playerId = candidatePlayerId;
                if (_players.ContainsKey(playerId))
                    throw new InvalidOperationException("Identifier provider returned a duplicate player ID.");

                var joinKind = _phase == SessionPhase.LoadingMatch || _phase == SessionPhase.Playing
                    ? SessionJoinKind.InProgress
                    : SessionJoinKind.Initial;
                _players.Add(playerId, new SessionPlayer(playerId, connection, joinKind));
                _playersByConnection.Add(connection, playerId);
                if (joinKind == SessionJoinKind.InProgress)
                {
                    timeout = new PlayerTimeoutContext(
                        playerId, connection, _operationId, _matchId, _configuration.PlayerLoadTimeout);
                    _playerTimeouts.Add(playerId, timeout);
                }

                DeriveLobbyPhase();
                AdvanceSnapshot();
                publication = EnqueuePublication(_snapshot);
                result = Result<PlayerId, SessionError>.Success(playerId);
                return Completed();
            }, token).ConfigureAwait(false);

            if (rejected)
            {
                Record(SessionTelemetryKind.CommandRejected, PlayerId.None, SessionError.SessionFull);
                return result;
            }

            if (timeout != null) _ = EnforcePlayerLoadTimeoutAsync(timeout);
            await PublishPendingAsync(publication).ConfigureAwait(false);
            return result;
        }

        public async ValueTask<Result<Unit, SessionError>> LeaveAsync(PlayerId playerId, CancellationToken token)
        {
            Task publication = null;
            PlayerTimeoutContext timeout = null;
            CancellationTokenSource playerWork = null;
            OperationId cancelledOperation = OperationId.None;
            WorldLoadContext cancelledLoad = null;
            WorldLoadContext completedLoad = null;
            List<PlayerTimeoutContext> cancelledTimeouts = null;
            List<CancellationTokenSource> cancelledWork = null;
            PlayerId[] additionallySpawned = null;
            var shouldDespawn = false;
            var changed = false;

            await _queue.ExecuteAsync(() =>
            {
                if (!_players.TryGetValue(playerId, out var player)) return Completed();

                shouldDespawn = player.SpawnState == SpawnState.Spawned;
                _players.Remove(playerId);
                _playersByConnection.Remove(player.Connection);
                _initialParticipants.Remove(playerId);
                _terminalDisconnectingPlayers.Remove(playerId);
                if (_playerTimeouts.TryGetValue(playerId, out timeout)) _playerTimeouts.Remove(playerId);
                if (_playerWork.TryGetValue(playerId, out playerWork)) _playerWork.Remove(playerId);

                if (_phase == SessionPhase.LoadingMatch && _players.Count < _configuration.MinPlayers)
                {
                    cancelledOperation = _operationId;
                    additionallySpawned = _players.Values
                        .Where(value => value.SpawnState == SpawnState.Spawned)
                        .Select(value => value.PlayerId).ToArray();
                    cancelledLoad = ResetInitialLoad();
                    cancelledTimeouts = DrainTimeouts();
                    cancelledWork = DrainPlayerWork();
                }
                else
                {
                    DeriveLobbyPhase();
                    TryEnterPlaying();
                    completedLoad = CompleteInitialLoadIfPlaying();
                }

                AdvanceSnapshot();
                publication = EnqueuePublication(_snapshot);
                changed = true;
                return Completed();
            }, token).ConfigureAwait(false);

            CancelAndDispose(timeout);
            Cancel(playerWork);
            CancelAndDispose(cancelledLoad);
            CancelAndDispose(completedLoad);
            CancelAndDispose(cancelledTimeouts);
            Cancel(cancelledWork);
            if (cancelledOperation.IsValid)
                await _worldProvider.CancelAsync(cancelledOperation, CancellationToken.None).ConfigureAwait(false);
            if (shouldDespawn)
                await _spawnProvider.DespawnAsync(playerId, CancellationToken.None).ConfigureAwait(false);
            if (additionallySpawned != null)
                for (var i = 0; i < additionallySpawned.Length; i++)
                    await _spawnProvider.DespawnAsync(additionallySpawned[i], CancellationToken.None)
                        .ConfigureAwait(false);
            if (changed)
            {
                await PublishPendingAsync(publication).ConfigureAwait(false);
                Record(SessionTelemetryKind.Disconnect, playerId, null);
            }

            return Result<Unit, SessionError>.Success(Unit.Value);
        }

        public async ValueTask<Result<Unit, SessionError>> SetReadyAsync(
            PlayerId playerId,
            bool ready,
            CancellationToken token)
        {
            var candidateMatchId = new MatchId(RequireNewGuid("match"));
            var candidateOperationId = new OperationId(RequireNewGuid("operation"));
            Task publication = null;
            MatchWorldLoadRequest loadRequest = default;
            WorldLoadContext loadContext = null;
            var result = Result<Unit, SessionError>.Success(Unit.Value);
            var rejected = false;

            await _queue.ExecuteAsync(() =>
            {
                if (!_players.TryGetValue(playerId, out var player))
                {
                    result = Result<Unit, SessionError>.Failure(SessionError.UnknownPlayer);
                    rejected = true;
                    return Completed();
                }

                if (player.Ready == ready) return Completed();
                if (_phase != SessionPhase.WaitingForPlayers && _phase != SessionPhase.Lobby)
                {
                    result = Result<Unit, SessionError>.Failure(SessionError.InvalidPhase);
                    rejected = true;
                    return Completed();
                }

                player.Ready = ready;
                if (CanCommitStart())
                {
                    loadRequest = CommitStart(candidateMatchId, candidateOperationId);
                    loadContext = _worldLoad;
                }

                AdvanceSnapshot();
                publication = EnqueuePublication(_snapshot);
                return Completed();
            }, token).ConfigureAwait(false);

            if (rejected)
            {
                Record(SessionTelemetryKind.CommandRejected, playerId, result.Error);
                return result;
            }

            await PublishPendingAsync(publication).ConfigureAwait(false);
            if (loadContext == null) return result;
            Record(SessionTelemetryKind.PhaseChanged, PlayerId.None, null);
            return await LoadWorldAsync(loadRequest, loadContext).ConfigureAwait(false);
        }

        public async ValueTask<Result<Unit, SessionError>> NotifyServerWorldReadyAsync(
            OperationId operationId,
            MatchId matchId,
            CancellationToken token)
        {
            Task publication = null;
            PlayerId[] readyPlayers = null;
            var result = Result<Unit, SessionError>.Success(Unit.Value);
            var changed = false;

            await _queue.ExecuteAsync(() =>
            {
                if (!MatchesActiveWorld(operationId, matchId))
                {
                    result = Result<Unit, SessionError>.Failure(SessionError.InvalidPhase);
                    return Completed();
                }

                if (!_serverWorldReady)
                {
                    _serverWorldReady = true;
                    TryEnterPlaying();
                    AdvanceSnapshot();
                    publication = EnqueuePublication(_snapshot);
                    changed = true;
                }

                readyPlayers = _players.Values.Where(player => player.WorldReady)
                    .Select(player => player.PlayerId).ToArray();
                return Completed();
            }, token).ConfigureAwait(false);

            if (result.IsFailure)
            {
                Record(SessionTelemetryKind.CommandRejected, PlayerId.None, result.Error);
                return result;
            }

            await PublishPendingAsync(publication).ConfigureAwait(false);
            if (changed) Record(SessionTelemetryKind.WorldLoad, PlayerId.None, null);
            for (var i = 0; i < readyPlayers.Length; i++)
            {
                var spawn = await TrySpawnAsync(readyPlayers[i]).ConfigureAwait(false);
                if (spawn.IsFailure) result = spawn;
            }

            return result;
        }

        public async ValueTask<Result<Unit, SessionError>> NotifyPlayerWorldReadyAsync(
            OperationId operationId,
            PlayerId playerId,
            MatchId matchId,
            CancellationToken token)
        {
            Task publication = null;
            PlayerTimeoutContext timeout = null;
            var result = Result<Unit, SessionError>.Success(Unit.Value);
            var changed = false;
            var shouldTrySpawn = false;

            await _queue.ExecuteAsync(() =>
            {
                if (!MatchesActiveWorld(operationId, matchId))
                {
                    result = Result<Unit, SessionError>.Failure(SessionError.InvalidPhase);
                    return Completed();
                }

                if (!_players.TryGetValue(playerId, out var player))
                {
                    result = Result<Unit, SessionError>.Failure(SessionError.UnknownPlayer);
                    return Completed();
                }

                if (_terminalDisconnectingPlayers.Contains(playerId))
                {
                    result = Result<Unit, SessionError>.Failure(SessionError.ConnectionClosed);
                    return Completed();
                }

                if (!player.WorldReady)
                {
                    player.WorldReady = true;
                    if (_playerTimeouts.TryGetValue(playerId, out timeout)) _playerTimeouts.Remove(playerId);
                    AdvanceSnapshot();
                    publication = EnqueuePublication(_snapshot);
                    changed = true;
                }

                shouldTrySpawn = _serverWorldReady;
                return Completed();
            }, token).ConfigureAwait(false);

            if (result.IsFailure)
            {
                Record(SessionTelemetryKind.CommandRejected, playerId, result.Error);
                return result;
            }

            CancelAndDispose(timeout);
            await PublishPendingAsync(publication).ConfigureAwait(false);
            if (changed) Record(SessionTelemetryKind.PlayerWorldReady, playerId, null);
            return shouldTrySpawn ? await TrySpawnAsync(playerId).ConfigureAwait(false) : result;
        }

        private async ValueTask<Result<Unit, SessionError>> LoadWorldAsync(
            MatchWorldLoadRequest request,
            WorldLoadContext context)
        {
            Task<Result<Unit, SessionError>> loadTask;
            try
            {
                loadTask = _worldProvider.LoadAsync(request, context.Token).AsTask();
            }
            catch
            {
                if (await IsLoadCompletedAsync(context).ConfigureAwait(false))
                    return Result<Unit, SessionError>.Success(Unit.Value);
                await RecoverInitialLoadAsync(request, SessionError.WorldLoadFailed).ConfigureAwait(false);
                throw;
            }

            var timeoutTask = EnforceWorldLoadTimeoutAsync(request, context).AsTask();
            Task winner;
            try
            {
                winner = await Task.WhenAny(loadTask, timeoutTask).ConfigureAwait(false);
                if (winner == loadTask)
                {
                    Result<Unit, SessionError> loadResult;
                    try
                    {
                        loadResult = await loadTask.ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (context.Token.IsCancellationRequested)
                    {
                        return await timeoutTask.ConfigureAwait(false);
                    }
                    catch
                    {
                        if (await IsLoadCompletedAsync(context).ConfigureAwait(false))
                            return Result<Unit, SessionError>.Success(Unit.Value);
                        await RecoverInitialLoadAsync(request, SessionError.WorldLoadFailed).ConfigureAwait(false);
                        throw;
                    }

                    if (loadResult.IsFailure)
                    {
                        var alreadyReady = await IsLoadCompletedAsync(context).ConfigureAwait(false);
                        if (alreadyReady)
                        {
                            ObserveFault(timeoutTask);
                            return Result<Unit, SessionError>.Success(Unit.Value);
                        }
                        var error = loadResult.Error == SessionError.WorldLoadTimeout
                            ? SessionError.WorldLoadTimeout
                            : SessionError.WorldLoadFailed;
                        await RecoverInitialLoadAsync(request, error).ConfigureAwait(false);
                        Record(SessionTelemetryKind.WorldLoad, PlayerId.None, error);
                        return Result<Unit, SessionError>.Failure(error);
                    }

                    ObserveFault(timeoutTask);
                    Record(SessionTelemetryKind.WorldLoad, PlayerId.None, null);
                    return Result<Unit, SessionError>.Success(Unit.Value);
                }

                ObserveFault(loadTask);
                return await timeoutTask.ConfigureAwait(false);
            }
            finally
            {
                ObserveFault(timeoutTask);
            }
        }

        private async ValueTask<Result<Unit, SessionError>> EnforceWorldLoadTimeoutAsync(
            MatchWorldLoadRequest request,
            WorldLoadContext context)
        {
            await WaitForCancellationAsync(context.Token).ConfigureAwait(false);
            var timedOut = false;
            var completed = false;
            await _queue.ExecuteAsync(() =>
            {
                completed = context.Completed;
                timedOut = !context.Completed && !context.Cancelled && _phase == SessionPhase.LoadingMatch &&
                           ReferenceEquals(_worldLoad, context) && MatchesActiveWorld(request.OperationId, request.MatchId);
                return Completed();
            }, CancellationToken.None).ConfigureAwait(false);

            if (completed) return Result<Unit, SessionError>.Success(Unit.Value);
            if (!timedOut) return Result<Unit, SessionError>.Failure(SessionError.InvalidPhase);
            await RecoverInitialLoadAsync(request, SessionError.WorldLoadTimeout).ConfigureAwait(false);
            if (await IsLoadCompletedAsync(context).ConfigureAwait(false))
                return Result<Unit, SessionError>.Success(Unit.Value);
            Record(SessionTelemetryKind.WorldLoad, PlayerId.None, SessionError.WorldLoadTimeout);
            return Result<Unit, SessionError>.Failure(SessionError.WorldLoadTimeout);
        }

        private async ValueTask<bool> IsLoadCompletedAsync(WorldLoadContext context)
        {
            var completed = false;
            await _queue.ExecuteAsync(() =>
            {
                completed = context.Completed;
                return Completed();
            }, CancellationToken.None).ConfigureAwait(false);
            return completed;
        }

        private async ValueTask RecoverInitialLoadAsync(MatchWorldLoadRequest request, SessionError error)
        {
            Task publication = null;
            WorldLoadContext load = null;
            List<PlayerTimeoutContext> timeouts = null;
            List<CancellationTokenSource> work = null;
            PlayerId[] spawned = null;
            var shouldCancel = false;

            await _queue.ExecuteAsync(() =>
            {
                if (_phase != SessionPhase.LoadingMatch ||
                    !MatchesActiveWorld(request.OperationId, request.MatchId)) return Completed();

                spawned = _players.Values.Where(player => player.SpawnState == SpawnState.Spawned)
                    .Select(player => player.PlayerId).ToArray();
                load = ResetInitialLoad();
                timeouts = DrainTimeouts();
                work = DrainPlayerWork();
                AdvanceSnapshot();
                publication = EnqueuePublication(_snapshot);
                shouldCancel = true;
                return Completed();
            }, CancellationToken.None).ConfigureAwait(false);

            if (!shouldCancel) return;
            CancelAndDispose(load);
            CancelAndDispose(timeouts);
            CancelAndDispose(work);
            await _worldProvider.CancelAsync(request.OperationId, CancellationToken.None).ConfigureAwait(false);
            for (var i = 0; i < spawned.Length; i++)
                await _spawnProvider.DespawnAsync(spawned[i], CancellationToken.None).ConfigureAwait(false);
            await PublishPendingAsync(publication).ConfigureAwait(false);
            Record(SessionTelemetryKind.PhaseChanged, PlayerId.None, error);
        }

        private async ValueTask<Result<Unit, SessionError>> TrySpawnAsync(PlayerId playerId)
        {
            Task publication = null;
            SpawnPlayerRequest request = default;
            CancellationTokenSource work = null;
            var prepared = false;

            await _queue.ExecuteAsync(() =>
            {
                if (!_serverWorldReady || !_players.TryGetValue(playerId, out var player) || !player.WorldReady ||
                    player.SpawnState == SpawnState.Spawning || player.SpawnState == SpawnState.Spawned)
                    return Completed();

                player.SpawnState = SpawnState.Spawning;
                work = new CancellationTokenSource();
                _playerWork[playerId] = work;
                request = new SpawnPlayerRequest(_operationId, _matchId, playerId, player.Connection);
                AdvanceSnapshot();
                publication = EnqueuePublication(_snapshot);
                prepared = true;
                return Completed();
            }, CancellationToken.None).ConfigureAwait(false);

            if (!prepared) return Result<Unit, SessionError>.Success(Unit.Value);
            await PublishPendingAsync(publication).ConfigureAwait(false);

            Result<SpawnPlayerResult, SessionError> spawnResult = default;
            for (var attempt = 0; attempt < 2; attempt++)
            {
                try
                {
                    spawnResult = await _spawnProvider.SpawnAsync(request, work.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (work.IsCancellationRequested)
                {
                    work.Dispose();
                    return Result<Unit, SessionError>.Failure(SessionError.InvalidPhase);
                }

                if (spawnResult.IsSuccess || attempt == 1) break;
                if (!await IsSpawnStillActiveAsync(request, work).ConfigureAwait(false))
                {
                    work.Dispose();
                    return Result<Unit, SessionError>.Failure(SessionError.InvalidPhase);
                }
            }

            Task appliedPublication = null;
            var staleSuccess = false;
            var applied = false;
            var enteredPlaying = false;
            WorldLoadContext completedLoad = null;
            await _queue.ExecuteAsync(() =>
            {
                if (_playerWork.TryGetValue(playerId, out var currentWork) && ReferenceEquals(currentWork, work))
                    _playerWork.Remove(playerId);

                if (!MatchesActiveWorld(request.OperationId, request.MatchId) ||
                    !_players.TryGetValue(playerId, out var player) ||
                    player.SpawnState != SpawnState.Spawning)
                {
                    staleSuccess = spawnResult.IsSuccess;
                    return Completed();
                }

                player.SpawnState = spawnResult.IsSuccess ? SpawnState.Spawned : SpawnState.NotSpawned;
                var oldPhase = _phase;
                TryEnterPlaying();
                enteredPlaying = oldPhase != _phase;
                completedLoad = CompleteInitialLoadIfPlaying();
                AdvanceSnapshot();
                appliedPublication = EnqueuePublication(_snapshot);
                applied = true;
                return Completed();
            }, CancellationToken.None).ConfigureAwait(false);

            work.Dispose();
            CancelAndDispose(completedLoad);
            if (staleSuccess)
                await _spawnProvider.DespawnAsync(playerId, CancellationToken.None).ConfigureAwait(false);
            if (!applied) return Result<Unit, SessionError>.Failure(SessionError.InvalidPhase);
            if (applied) await PublishPendingAsync(appliedPublication).ConfigureAwait(false);
            if (enteredPlaying) Record(SessionTelemetryKind.PhaseChanged, PlayerId.None, null);
            Record(SessionTelemetryKind.Spawn, playerId,
                spawnResult.IsFailure ? SessionError.SpawnFailed : (SessionError?)null);
            if (spawnResult.IsSuccess) return Result<Unit, SessionError>.Success(Unit.Value);

            await _connectionProvider.DisconnectAsync(
                request.Connection, SessionError.SpawnFailed, CancellationToken.None).ConfigureAwait(false);
            await LeaveAsync(playerId, CancellationToken.None).ConfigureAwait(false);
            return Result<Unit, SessionError>.Failure(SessionError.SpawnFailed);
        }

        private async ValueTask<bool> IsSpawnStillActiveAsync(
            SpawnPlayerRequest request,
            CancellationTokenSource work)
        {
            var active = false;
            await _queue.ExecuteAsync(() =>
            {
                active = MatchesActiveWorld(request.OperationId, request.MatchId) &&
                         _players.TryGetValue(request.PlayerId, out var player) &&
                         player.SpawnState == SpawnState.Spawning &&
                         _playerWork.TryGetValue(request.PlayerId, out var current) &&
                         ReferenceEquals(current, work);
                return Completed();
            }, CancellationToken.None).ConfigureAwait(false);
            return active;
        }

        private async Task EnforcePlayerLoadTimeoutAsync(PlayerTimeoutContext context)
        {
            try
            {
                await WaitForCancellationAsync(context.Source.Token).ConfigureAwait(false);
                var shouldDisconnect = false;
                await _queue.ExecuteAsync(() =>
                {
                    shouldDisconnect = !context.Completed && !context.Cancelled &&
                        _playerTimeouts.TryGetValue(context.PlayerId, out var active) &&
                        ReferenceEquals(active, context) &&
                        MatchesActiveWorld(context.OperationId, context.MatchId) &&
                        _players.TryGetValue(context.PlayerId, out var player) && !player.WorldReady;
                    if (shouldDisconnect) _playerTimeouts.Remove(context.PlayerId);
                    if (shouldDisconnect) _terminalDisconnectingPlayers.Add(context.PlayerId);
                    return Completed();
                }, CancellationToken.None).ConfigureAwait(false);

                if (shouldDisconnect)
                {
                    context.Completed = true;
                    await _connectionProvider.DisconnectAsync(
                        context.Connection, SessionError.PlayerLoadTimeout, CancellationToken.None)
                        .ConfigureAwait(false);
                }
            }
            finally
            {
                context.Source.Dispose();
            }
        }

        private bool CanCommitStart()
        {
            return _phase == SessionPhase.Lobby && _players.Count >= _configuration.MinPlayers &&
                   _players.Values.All(player => player.Ready);
        }

        private MatchWorldLoadRequest CommitStart(MatchId matchId, OperationId operationId)
        {
            _phase = SessionPhase.LoadingMatch;
            _matchId = matchId;
            _operationId = operationId;
            _mapId = _configuration.MapId;
            _serverWorldReady = false;
            _initialParticipants.Clear();
            _initialParticipants.UnionWith(_players.Keys);
            _worldLoad = new WorldLoadContext(_operationId, _matchId, _configuration.WorldLoadTimeout);
            return new MatchWorldLoadRequest(_operationId, _matchId, _mapId);
        }

        private WorldLoadContext ResetInitialLoad()
        {
            var load = _worldLoad;
            if (load != null) load.Cancelled = true;
            _worldLoad = null;
            _operationId = OperationId.None;
            _matchId = MatchId.None;
            _mapId = MapId.None;
            _serverWorldReady = false;
            _initialParticipants.Clear();
            foreach (var player in _players.Values)
            {
                player.Ready = false;
                player.WorldReady = false;
                player.SpawnState = SpawnState.NotSpawned;
            }
            _phase = _players.Count < _configuration.MinPlayers
                ? SessionPhase.WaitingForPlayers
                : SessionPhase.Lobby;
            return load;
        }

        private bool MatchesActiveWorld(OperationId operationId, MatchId matchId)
        {
            return (_phase == SessionPhase.LoadingMatch || _phase == SessionPhase.Playing) &&
                   operationId.IsValid && matchId.IsValid &&
                   operationId == _operationId && matchId == _matchId;
        }

        private void TryEnterPlaying()
        {
            if (_phase != SessionPhase.LoadingMatch || !_serverWorldReady) return;
            foreach (var playerId in _initialParticipants)
            {
                if (_players.TryGetValue(playerId, out var player) && player.SpawnState != SpawnState.Spawned)
                    return;
            }
            _phase = SessionPhase.Playing;
        }

        private WorldLoadContext CompleteInitialLoadIfPlaying()
        {
            if (_phase != SessionPhase.Playing || _worldLoad == null) return null;
            var completed = _worldLoad;
            completed.Completed = true;
            _worldLoad = null;
            return completed;
        }

        private void DeriveLobbyPhase()
        {
            if (_phase == SessionPhase.WaitingForPlayers || _phase == SessionPhase.Lobby)
                _phase = _players.Count < _configuration.MinPlayers
                    ? SessionPhase.WaitingForPlayers
                    : SessionPhase.Lobby;
        }

        private List<PlayerTimeoutContext> DrainTimeouts()
        {
            var values = _playerTimeouts.Values.ToList();
            _playerTimeouts.Clear();
            for (var i = 0; i < values.Count; i++) values[i].Cancelled = true;
            return values;
        }

        private List<CancellationTokenSource> DrainPlayerWork()
        {
            var values = _playerWork.Values.ToList();
            _playerWork.Clear();
            return values;
        }

        private void AdvanceSnapshot()
        {
            _revision++;
            Volatile.Write(ref _snapshot, BuildSnapshot());
        }

        private SessionSnapshot BuildSnapshot()
        {
            var players = _players.Values.OrderBy(player => player.PlayerId.Value)
                .Select(player => player.ToSnapshot()).ToArray();
            return new SessionSnapshot(
                _sessionId, _matchId, _mapId, _phase, _revision, _serverWorldReady, players);
        }

        private Task EnqueuePublication(SessionSnapshot snapshot)
        {
            var item = new PublicationItem(snapshot);
            lock (_publicationSync) _pendingPublications.Enqueue(item);
            return item.Completion.Task;
        }

        private async Task PublishPendingAsync(Task requestedPublication)
        {
            if (requestedPublication == null || _isPublishing.Value) return;
            await _publicationGate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
            try
            {
                while (TryDequeuePublication(out var item))
                {
                    try
                    {
                        _isPublishing.Value = true;
                        await _eventPublisher.PublishSnapshotAsync(item.Snapshot, CancellationToken.None)
                            .ConfigureAwait(false);
                        item.Completion.TrySetResult(Unit.Value);
                    }
                    catch (Exception exception)
                    {
                        item.Completion.TrySetException(exception);
                    }
                    finally
                    {
                        _isPublishing.Value = false;
                    }
                }
            }
            finally
            {
                _publicationGate.Release();
            }
            await requestedPublication.ConfigureAwait(false);
        }

        private bool TryDequeuePublication(out PublicationItem item)
        {
            lock (_publicationSync)
            {
                if (_pendingPublications.Count == 0)
                {
                    item = null;
                    return false;
                }
                item = _pendingPublications.Dequeue();
                return true;
            }
        }

        private Guid RequireNewGuid(string kind)
        {
            var value = _identifiers.NewGuid();
            if (value == Guid.Empty)
                throw new InvalidOperationException($"Identifier provider returned an empty {kind} ID.");
            return value;
        }

        private void Record(SessionTelemetryKind kind, PlayerId playerId, SessionError? error)
        {
            var snapshot = Snapshot;
            _telemetry.Record(new SessionTelemetryEvent(
                kind, snapshot.SessionId, snapshot.MatchId, _operationId, playerId,
                snapshot.Phase, snapshot.Revision, error));
        }

        private static ValueTask<Unit> Completed() => new ValueTask<Unit>(Unit.Value);

        private static Task WaitForCancellationAsync(CancellationToken token)
        {
            if (token.IsCancellationRequested) return Task.CompletedTask;
            var completion = new TaskCompletionSource<Unit>(TaskCreationOptions.RunContinuationsAsynchronously);
            token.Register(() => completion.TrySetResult(Unit.Value));
            return completion.Task;
        }

        private static void ObserveFault(Task task)
        {
            if (task == null) return;
            _ = task.ContinueWith(
                completed => { var ignored = completed.Exception; },
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        private static void CancelAndDispose(WorldLoadContext context)
        {
            if (context == null) return;
            context.Source.Cancel();
            context.Source.Dispose();
        }

        private static void CancelAndDispose(PlayerTimeoutContext context)
        {
            if (context == null) return;
            context.Cancelled = true;
            context.Source.Cancel();
        }

        private static void CancelAndDispose(CancellationTokenSource source)
        {
            if (source == null) return;
            source.Cancel();
            source.Dispose();
        }

        private static void CancelAndDispose(IEnumerable<PlayerTimeoutContext> contexts)
        {
            if (contexts == null) return;
            foreach (var context in contexts) CancelAndDispose(context);
        }

        private static void CancelAndDispose(IEnumerable<CancellationTokenSource> sources)
        {
            if (sources == null) return;
            foreach (var source in sources) CancelAndDispose(source);
        }

        private static void Cancel(CancellationTokenSource source)
        {
            source?.Cancel();
        }

        private static void Cancel(IEnumerable<CancellationTokenSource> sources)
        {
            if (sources == null) return;
            foreach (var source in sources) Cancel(source);
        }

        private sealed class WorldLoadContext
        {
            public WorldLoadContext(OperationId operationId, MatchId matchId, TimeSpan timeout)
            {
                OperationId = operationId;
                MatchId = matchId;
                Source = new CancellationTokenSource();
                Token = Source.Token;
                Source.CancelAfter(timeout);
            }
            public OperationId OperationId { get; }
            public MatchId MatchId { get; }
            public CancellationTokenSource Source { get; }
            public CancellationToken Token { get; }
            public bool Completed { get; set; }
            public bool Cancelled { get; set; }
        }

        private sealed class PlayerTimeoutContext
        {
            public PlayerTimeoutContext(
                PlayerId playerId,
                SessionConnection connection,
                OperationId operationId,
                MatchId matchId,
                TimeSpan timeout)
            {
                PlayerId = playerId;
                Connection = connection;
                OperationId = operationId;
                MatchId = matchId;
                Source = new CancellationTokenSource();
                Source.CancelAfter(timeout);
            }
            public PlayerId PlayerId { get; }
            public SessionConnection Connection { get; }
            public OperationId OperationId { get; }
            public MatchId MatchId { get; }
            public CancellationTokenSource Source { get; }
            public bool Completed { get; set; }
            public bool Cancelled { get; set; }
        }

        private sealed class PublicationItem
        {
            public PublicationItem(SessionSnapshot snapshot)
            {
                Snapshot = snapshot;
                Completion = new TaskCompletionSource<Unit>(TaskCreationOptions.RunContinuationsAsynchronously);
            }
            public SessionSnapshot Snapshot { get; }
            public TaskCompletionSource<Unit> Completion { get; }
        }
    }
}
