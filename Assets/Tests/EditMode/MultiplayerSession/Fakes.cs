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
            FakeSessionEventPublisher publisher)
        {
            Service = service;
            World = world;
            Publisher = publisher;
        }

        public ServerSessionFacade Service { get; }
        public FakeMatchWorldProvider World { get; }
        public FakeSessionEventPublisher Publisher { get; }

        public static SessionFixture Create(bool worldCallsReady = false)
        {
            var world = new FakeMatchWorldProvider(worldCallsReady);
            var publisher = new FakeSessionEventPublisher();
            var service = new ServerSessionFacade(
                new SessionConfiguration(
                    10,
                    2,
                    new MapId(new Guid("10000000-0000-0000-0000-000000000001")),
                    TimeSpan.FromSeconds(15),
                    TimeSpan.FromSeconds(10)),
                new SessionCommandQueue(),
                world,
                new FakePlayerSpawnProvider(),
                publisher,
                new FakeSessionConnectionProvider(),
                NullSessionTelemetry.Instance,
                new SequentialIdentifierProvider());
            world.Service = service;
            return new SessionFixture(service, world, publisher);
        }

        public ValueTask<Result<PlayerId, SessionError>> JoinAsync()
        {
            _connectionSequence++;
            var bytes = new byte[16];
            BitConverter.GetBytes(_connectionSequence).CopyTo(bytes, 0);
            return Service.JoinAsync(new SessionConnection(new Guid(bytes)), default);
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

        public FakeMatchWorldProvider(bool callsReady)
        {
            _callsReady = callsReady;
        }

        public IServerSessionFacade Service { get; set; }
        public List<MatchWorldLoadRequest> LoadRequests { get; } = new List<MatchWorldLoadRequest>();
        public List<OperationId> CancelledOperations { get; } = new List<OperationId>();

        public async ValueTask<Result<Unit, SessionError>> LoadAsync(
            MatchWorldLoadRequest request,
            CancellationToken token)
        {
            LoadRequests.Add(request);
            if (_callsReady)
                await Service.NotifyServerWorldReadyAsync(request.OperationId, request.MatchId, token);
            return Result<Unit, SessionError>.Success(Unit.Value);
        }

        public ValueTask CancelAsync(OperationId operationId, CancellationToken token)
        {
            CancelledOperations.Add(operationId);
            return default;
        }
    }

    internal sealed class FakePlayerSpawnProvider : IPlayerSpawnProvider
    {
        public ValueTask<Result<SpawnPlayerResult, SessionError>> SpawnAsync(
            SpawnPlayerRequest request,
            CancellationToken token)
        {
            return new ValueTask<Result<SpawnPlayerResult, SessionError>>(
                Result<SpawnPlayerResult, SessionError>.Success(SpawnPlayerResult.Completed));
        }

        public ValueTask<Result<Unit, SessionError>> DespawnAsync(PlayerId playerId, CancellationToken token)
        {
            return new ValueTask<Result<Unit, SessionError>>(
                Result<Unit, SessionError>.Success(Unit.Value));
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
        public ValueTask DisconnectAsync(
            SessionConnection connection,
            SessionError reason,
            CancellationToken token)
        {
            return default;
        }
    }
}
