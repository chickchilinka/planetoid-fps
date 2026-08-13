using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Modules.Multiplayer.Primitives;

namespace Modules.Multiplayer.Session.Tests
{
    internal sealed class SessionFixture
    {
        private int _connectionSequence;

        private SessionFixture(
            ServerSessionFacade service,
            FakeMatchWorldProvider world,
            FakePlayerSpawnProvider spawn,
            FakeSessionConnectionProvider connections,
            FakeSessionEventPublisher publisher)
        {
            Service = service;
            World = world;
            Spawn = spawn;
            Connections = connections;
            Publisher = publisher;
        }

        public ServerSessionFacade Service { get; }
        public FakeMatchWorldProvider World { get; }
        public FakePlayerSpawnProvider Spawn { get; }
        public FakeSessionConnectionProvider Connections { get; }
        public FakeSessionEventPublisher Publisher { get; }
        public Dictionary<PlayerId, SessionConnection> PlayerConnections { get; } =
            new Dictionary<PlayerId, SessionConnection>();

        public static SessionFixture Create(
            bool worldCallsReady = false,
            bool worldCallsAllReady = false,
            TimeSpan? worldLoadTimeout = null,
            TimeSpan? playerLoadTimeout = null)
        {
            var world = new FakeMatchWorldProvider(worldCallsReady, worldCallsAllReady);
            var spawn = new FakePlayerSpawnProvider();
            var connections = new FakeSessionConnectionProvider();
            var publisher = new FakeSessionEventPublisher();
            var service = new ServerSessionFacade(
                new SessionConfiguration(
                    10,
                    2,
                    new MapId(new Guid("10000000-0000-0000-0000-000000000001")),
                    worldLoadTimeout ?? TimeSpan.FromSeconds(15),
                    playerLoadTimeout ?? TimeSpan.FromSeconds(10)),
                new SessionCommandQueue(),
                world,
                spawn,
                publisher,
                connections,
                NullSessionTelemetry.Instance,
                new SequentialIdentifierProvider());
            world.Service = service;
            return new SessionFixture(service, world, spawn, connections, publisher);
        }

        public async ValueTask<Result<PlayerId, SessionError>> JoinAsync()
        {
            _connectionSequence++;
            var bytes = new byte[16];
            BitConverter.GetBytes(_connectionSequence).CopyTo(bytes, 0);
            var connection = new SessionConnection(new Guid(bytes));
            var result = await Service.JoinAsync(connection, default);
            if (result.IsSuccess) PlayerConnections[result.Value] = connection;
            return result;
        }
    }

    internal sealed class SequentialIdentifierProvider : IIdentifierProvider
    {
        private int _sequence;

        public Guid NewGuid()
        {
            _sequence++;
            var bytes = new byte[16];
            BitConverter.GetBytes(_sequence).CopyTo(bytes, 0);
            bytes[15] = 1;
            return new Guid(bytes);
        }
    }

    internal sealed class FakeMatchWorldProvider : IMatchWorldProvider
    {
        private readonly bool _callsReady;
        private readonly bool _callsAllReady;

        public FakeMatchWorldProvider(bool callsReady, bool callsAllReady)
        {
            _callsReady = callsReady;
            _callsAllReady = callsAllReady;
        }

        public IServerSessionFacade Service { get; set; }
        public List<MatchWorldLoadRequest> LoadRequests { get; } = new List<MatchWorldLoadRequest>();
        public List<OperationId> CancelledOperations { get; } = new List<OperationId>();
        public Result<Unit, SessionError> LoadResult { get; set; } =
            Result<Unit, SessionError>.Success(Unit.Value);
        public bool BlockLoad { get; set; }
        public bool IgnoreCancellation { get; set; }
        public TaskCompletionSource<Result<Unit, SessionError>> LoadCompletion { get; } =
            new TaskCompletionSource<Result<Unit, SessionError>>(TaskCreationOptions.RunContinuationsAsynchronously);

        public async ValueTask<Result<Unit, SessionError>> LoadAsync(
            MatchWorldLoadRequest request,
            CancellationToken token)
        {
            LoadRequests.Add(request);
            if (_callsAllReady)
            {
                var players = Service.Snapshot.Players;
                for (var i = 0; i < players.Count; i++)
                    await Service.NotifyPlayerWorldReadyAsync(
                        request.OperationId, players[i].PlayerId, request.MatchId, token);
            }
            if (_callsReady)
                await Service.NotifyServerWorldReadyAsync(request.OperationId, request.MatchId, token);
            if (_callsAllReady)
                await Service.NotifyServerWorldReadyAsync(request.OperationId, request.MatchId, token);
            if (!BlockLoad) return LoadResult;
            if (IgnoreCancellation) return await LoadCompletion.Task;
            return await WithCancellation(LoadCompletion.Task, token);
        }

        public ValueTask CancelAsync(OperationId operationId, CancellationToken token)
        {
            CancelledOperations.Add(operationId);
            return default;
        }

        private static async Task<T> WithCancellation<T>(Task<T> task, CancellationToken token)
        {
            var cancellation = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (token.Register(() => cancellation.TrySetCanceled(token)))
                return await await Task.WhenAny(task, cancellation.Task);
        }
    }

    internal sealed class FakePlayerSpawnProvider : IPlayerSpawnProvider
    {
        public List<SpawnPlayerRequest> SpawnRequests { get; } = new List<SpawnPlayerRequest>();
        public List<PlayerId> DespawnRequests { get; } = new List<PlayerId>();
        public Result<SpawnPlayerResult, SessionError> SpawnResult { get; set; } =
            Result<SpawnPlayerResult, SessionError>.Success(SpawnPlayerResult.Completed);
        public bool BlockSpawn { get; set; }
        public bool IgnoreCancellation { get; set; }
        public TaskCompletionSource<Result<SpawnPlayerResult, SessionError>> SpawnCompletion { get; } =
            new TaskCompletionSource<Result<SpawnPlayerResult, SessionError>>(
                TaskCreationOptions.RunContinuationsAsynchronously);

        public async ValueTask<Result<SpawnPlayerResult, SessionError>> SpawnAsync(
            SpawnPlayerRequest request,
            CancellationToken token)
        {
            SpawnRequests.Add(request);
            if (!BlockSpawn) return SpawnResult;
            if (IgnoreCancellation) return await SpawnCompletion.Task;
            return await WithCancellation(SpawnCompletion.Task, token);
        }

        public ValueTask<Result<Unit, SessionError>> DespawnAsync(PlayerId playerId, CancellationToken token)
        {
            DespawnRequests.Add(playerId);
            return new ValueTask<Result<Unit, SessionError>>(
                Result<Unit, SessionError>.Success(Unit.Value));
        }

        private static async Task<T> WithCancellation<T>(Task<T> task, CancellationToken token)
        {
            var cancellation = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (token.Register(() => cancellation.TrySetCanceled(token)))
                return await await Task.WhenAny(task, cancellation.Task);
        }
    }

    internal sealed class FakeSessionEventPublisher : ISessionEventPublisher
    {
        private readonly object _sync = new object();

        public List<SessionSnapshot> Snapshots { get; } = new List<SessionSnapshot>();

        public ValueTask PublishSnapshotAsync(SessionSnapshot snapshot, CancellationToken token)
        {
            lock (_sync) Snapshots.Add(snapshot);
            return default;
        }
    }

    internal sealed class FakeSessionConnectionProvider : ISessionConnectionProvider
    {
        private readonly object _sync = new object();

        public List<(SessionConnection Connection, SessionError Reason)> Disconnects { get; } =
            new List<(SessionConnection, SessionError)>();

        public ValueTask DisconnectAsync(
            SessionConnection connection,
            SessionError reason,
            CancellationToken token)
        {
            lock (_sync) Disconnects.Add((connection, reason));
            return default;
        }
    }
}
