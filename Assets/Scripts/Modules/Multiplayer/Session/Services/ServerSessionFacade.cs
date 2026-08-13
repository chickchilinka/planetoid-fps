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
        private readonly object _publicationSync = new object();
        private readonly Queue<PublicationItem> _pendingPublications = new Queue<PublicationItem>();
        private readonly SemaphoreSlim _publicationGate = new SemaphoreSlim(1, 1);
        private readonly AsyncLocal<bool> _isPublishing = new AsyncLocal<bool>();
        private SessionSnapshot _snapshot;
        private SessionPhase _phase = SessionPhase.WaitingForPlayers;
        private readonly SessionId _sessionId;
        private MatchId _matchId = MatchId.None;
        private MapId _mapId = MapId.None;
        private OperationId _operationId = OperationId.None;
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

            Task publication = null;
            Result<PlayerId, SessionError> result = default;
            var rejected = false;
            await _queue.ExecuteAsync(() =>
            {
                if (_playersByConnection.TryGetValue(connection, out var existing))
                {
                    result = Result<PlayerId, SessionError>.Success(existing);
                    return new ValueTask<Unit>(Unit.Value);
                }

                if (_players.Count >= _configuration.MaxPlayers)
                {
                    rejected = true;
                    result = Result<PlayerId, SessionError>.Failure(SessionError.SessionFull);
                    return new ValueTask<Unit>(Unit.Value);
                }

                var playerId = new PlayerId(RequireNewGuid("player"));
                if (_players.ContainsKey(playerId))
                    throw new InvalidOperationException("Identifier provider returned a duplicate player ID.");

                var joinKind = _phase == SessionPhase.LoadingMatch || _phase == SessionPhase.Playing
                    ? SessionJoinKind.InProgress
                    : SessionJoinKind.Initial;
                _players.Add(playerId, new SessionPlayer(playerId, connection, joinKind));
                _playersByConnection.Add(connection, playerId);
                DeriveLobbyPhase();
                AdvanceSnapshot();
                publication = EnqueuePublication(_snapshot);
                result = Result<PlayerId, SessionError>.Success(playerId);
                return new ValueTask<Unit>(Unit.Value);
            }, token).ConfigureAwait(false);

            if (rejected)
            {
                Record(SessionTelemetryKind.CommandRejected, PlayerId.None, SessionError.SessionFull);
                return result;
            }

            await PublishPendingAsync(publication).ConfigureAwait(false);
            return result;
        }

        public async ValueTask<Result<Unit, SessionError>> LeaveAsync(PlayerId playerId, CancellationToken token)
        {
            Task publication = null;
            var changed = false;
            await _queue.ExecuteAsync(() =>
            {
                if (!_players.TryGetValue(playerId, out var player))
                    return new ValueTask<Unit>(Unit.Value);

                _players.Remove(playerId);
                _playersByConnection.Remove(player.Connection);
                _initialParticipants.Remove(playerId);
                DeriveLobbyPhase();
                AdvanceSnapshot();
                publication = EnqueuePublication(_snapshot);
                changed = true;
                return new ValueTask<Unit>(Unit.Value);
            }, token).ConfigureAwait(false);

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
            Task publication = null;
            var result = Result<Unit, SessionError>.Success(Unit.Value);
            var rejected = false;
            var hasLoad = false;
            MatchWorldLoadRequest loadRequest = default;

            await _queue.ExecuteAsync(() =>
            {
                if (!_players.TryGetValue(playerId, out var player))
                {
                    result = Result<Unit, SessionError>.Failure(SessionError.UnknownPlayer);
                    rejected = true;
                    return new ValueTask<Unit>(Unit.Value);
                }

                if (player.Ready == ready)
                {
                    return new ValueTask<Unit>(Unit.Value);
                }

                if (_phase != SessionPhase.WaitingForPlayers && _phase != SessionPhase.Lobby)
                {
                    result = Result<Unit, SessionError>.Failure(SessionError.InvalidPhase);
                    rejected = true;
                    return new ValueTask<Unit>(Unit.Value);
                }

                player.Ready = ready;
                if (CanCommitStart())
                {
                    loadRequest = CommitStart();
                    hasLoad = true;
                }

                AdvanceSnapshot();
                publication = EnqueuePublication(_snapshot);
                return new ValueTask<Unit>(Unit.Value);
            }, token).ConfigureAwait(false);

            if (rejected)
            {
                Record(SessionTelemetryKind.CommandRejected, playerId, result.Error);
                return result;
            }

            await PublishPendingAsync(publication).ConfigureAwait(false);
            if (!hasLoad) return result;

            Record(SessionTelemetryKind.PhaseChanged, PlayerId.None, null);
            var loadResult = await _worldProvider.LoadAsync(loadRequest, token).ConfigureAwait(false);
            Record(SessionTelemetryKind.WorldLoad, PlayerId.None, loadResult.IsFailure ? loadResult.Error : null);
            return loadResult;
        }

        public async ValueTask<Result<Unit, SessionError>> NotifyServerWorldReadyAsync(
            OperationId operationId,
            MatchId matchId,
            CancellationToken token)
        {
            Task publication = null;
            var result = Result<Unit, SessionError>.Success(Unit.Value);
            var changed = false;
            await _queue.ExecuteAsync(() =>
            {
                if (!MatchesActiveWorld(operationId, matchId))
                {
                    result = Result<Unit, SessionError>.Failure(SessionError.InvalidPhase);
                    return new ValueTask<Unit>(Unit.Value);
                }

                if (_serverWorldReady)
                {
                    return new ValueTask<Unit>(Unit.Value);
                }

                _serverWorldReady = true;
                AdvanceSnapshot();
                publication = EnqueuePublication(_snapshot);
                changed = true;
                return new ValueTask<Unit>(Unit.Value);
            }, token).ConfigureAwait(false);

            if (result.IsFailure)
            {
                Record(SessionTelemetryKind.CommandRejected, PlayerId.None, result.Error);
                return result;
            }

            await PublishPendingAsync(publication).ConfigureAwait(false);
            if (changed) Record(SessionTelemetryKind.WorldLoad, PlayerId.None, null);
            return result;
        }

        public async ValueTask<Result<Unit, SessionError>> NotifyPlayerWorldReadyAsync(
            OperationId operationId,
            PlayerId playerId,
            MatchId matchId,
            CancellationToken token)
        {
            Task publication = null;
            var result = Result<Unit, SessionError>.Success(Unit.Value);
            var changed = false;
            await _queue.ExecuteAsync(() =>
            {
                if (!MatchesActiveWorld(operationId, matchId))
                {
                    result = Result<Unit, SessionError>.Failure(SessionError.InvalidPhase);
                    return new ValueTask<Unit>(Unit.Value);
                }

                if (!_players.TryGetValue(playerId, out var player))
                {
                    result = Result<Unit, SessionError>.Failure(SessionError.UnknownPlayer);
                    return new ValueTask<Unit>(Unit.Value);
                }

                if (player.WorldReady)
                {
                    return new ValueTask<Unit>(Unit.Value);
                }

                player.WorldReady = true;
                AdvanceSnapshot();
                publication = EnqueuePublication(_snapshot);
                changed = true;
                return new ValueTask<Unit>(Unit.Value);
            }, token).ConfigureAwait(false);

            if (result.IsFailure)
            {
                Record(SessionTelemetryKind.CommandRejected, playerId, result.Error);
                return result;
            }

            await PublishPendingAsync(publication).ConfigureAwait(false);
            if (changed) Record(SessionTelemetryKind.PlayerWorldReady, playerId, null);
            return result;
        }

        private bool CanCommitStart()
        {
            return _phase == SessionPhase.Lobby && _players.Count >= _configuration.MinPlayers &&
                   _players.Values.All(player => player.Ready);
        }

        private MatchWorldLoadRequest CommitStart()
        {
            _phase = SessionPhase.LoadingMatch;
            _matchId = new MatchId(RequireNewGuid("match"));
            _operationId = new OperationId(RequireNewGuid("operation"));
            _mapId = _configuration.MapId;
            _serverWorldReady = false;
            _initialParticipants.Clear();
            _initialParticipants.UnionWith(_players.Keys);
            return new MatchWorldLoadRequest(_operationId, _matchId, _mapId);
        }

        private bool MatchesActiveWorld(OperationId operationId, MatchId matchId)
        {
            return (_phase == SessionPhase.LoadingMatch || _phase == SessionPhase.Playing) &&
                   operationId.IsValid && matchId.IsValid &&
                   operationId == _operationId && matchId == _matchId;
        }

        private void DeriveLobbyPhase()
        {
            if (_phase == SessionPhase.WaitingForPlayers || _phase == SessionPhase.Lobby)
                _phase = _players.Count < _configuration.MinPlayers
                    ? SessionPhase.WaitingForPlayers
                    : SessionPhase.Lobby;
        }

        private void AdvanceSnapshot()
        {
            _revision++;
            Volatile.Write(ref _snapshot, BuildSnapshot());
        }

        private SessionSnapshot BuildSnapshot()
        {
            var players = _players.Values
                .OrderBy(player => player.PlayerId.Value)
                .Select(player => player.ToSnapshot())
                .ToArray();
            return new SessionSnapshot(
                _sessionId,
                _matchId,
                _mapId,
                _phase,
                _revision,
                _serverWorldReady,
                players);
        }

        private Task EnqueuePublication(SessionSnapshot snapshot)
        {
            var item = new PublicationItem(snapshot);
            lock (_publicationSync)
            {
                _pendingPublications.Enqueue(item);
            }

            return item.Completion.Task;
        }

        private async Task PublishPendingAsync(Task requestedPublication)
        {
            if (requestedPublication == null) return;
            if (_isPublishing.Value) return;

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
                kind,
                snapshot.SessionId,
                snapshot.MatchId,
                _operationId,
                playerId,
                snapshot.Phase,
                snapshot.Revision,
                error));
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
