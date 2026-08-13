using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Modules.Multiplayer.Primitives;

namespace Modules.Multiplayer.Spawning.Tests
{
    internal sealed class SpawnFixture
    {
        private SpawnFixture(PlayerSpawnService service, FakeSpawnPointProvider points, FakePlayerEntityRuntimeProvider runtime, FakeSpawnTelemetry telemetry)
        {
            Service = service;
            Points = points;
            Runtime = runtime;
            Telemetry = telemetry;
        }

        public PlayerSpawnService Service { get; }
        public FakeSpawnPointProvider Points { get; }
        public FakePlayerEntityRuntimeProvider Runtime { get; }
        public FakeSpawnTelemetry Telemetry { get; }
        public PlayerSpawnRequest Request { get; } = new PlayerSpawnRequest(
            new PlayerId(new Guid("10000000-0000-0000-0000-000000000001")),
            new MatchId(new Guid("20000000-0000-0000-0000-000000000001")));

        public static SpawnFixture Successful()
        {
            var points = new FakeSpawnPointProvider("spawn-a", "spawn-b");
            var runtime = new FakePlayerEntityRuntimeProvider();
            var telemetry = new FakeSpawnTelemetry();
            return new SpawnFixture(new PlayerSpawnService(points, runtime, telemetry), points, runtime, telemetry);
        }

        public static SpawnFixture FailFirstCreation()
        {
            var fixture = Successful();
            fixture.Runtime.CreateResults.Enqueue(Result<RuntimeEntityHandle, SpawnError>.Failure(SpawnError.EntityCreationFailed));
            fixture.Runtime.CreateResults.Enqueue(Result<RuntimeEntityHandle, SpawnError>.Success(new RuntimeEntityHandle(new Guid("30000000-0000-0000-0000-000000000002"))));
            return fixture;
        }

        public static PlayerId Player(string value)
        {
            return new PlayerId(Guid.Parse(value));
        }
    }

    internal sealed class FakeSpawnPointProvider : ISpawnPointProvider
    {
        private readonly Queue<SpawnReservation> _available = new Queue<SpawnReservation>();

        public FakeSpawnPointProvider(params string[] pointIds)
        {
            for (var index = 0; index < pointIds.Length; index++)
            {
                _available.Enqueue(new SpawnReservation(
                    pointIds[index],
                    new SpawnPose(index, 0f, 0f, 0f, 0f, 0f, 1f)));
            }
        }

        public List<string> ReservedIds { get; } = new List<string>();
        public List<string> ReleasedIds { get; } = new List<string>();

        public Result<SpawnReservation, SpawnError> Reserve(PlayerId playerId, IReadOnlyCollection<string> excludedPointIds)
        {
            while (_available.Count > 0)
            {
                var reservation = _available.Dequeue();
                if (excludedPointIds.Contains(reservation.PointId)) continue;
                ReservedIds.Add(reservation.PointId);
                return Result<SpawnReservation, SpawnError>.Success(reservation);
            }

            return Result<SpawnReservation, SpawnError>.Failure(SpawnError.NoSpawnPoint);
        }

        public void Release(SpawnReservation reservation)
        {
            ReleasedIds.Add(reservation.PointId);
        }
    }

    internal sealed class FakePlayerEntityRuntimeProvider : IPlayerEntityRuntimeProvider
    {
        private int _created;

        public int CreateCalls { get; private set; }
        public int DestroyCalls { get; private set; }
        public Queue<Result<RuntimeEntityHandle, SpawnError>> CreateResults { get; } = new Queue<Result<RuntimeEntityHandle, SpawnError>>();
        public Result<Unit, SpawnError> DestroyResult { get; set; } = Result<Unit, SpawnError>.Success(Unit.Value);
        public bool ThrowOnCreateWhenCancelled { get; set; }
        public bool BlockCreate { get; set; }
        public TaskCompletionSource<Unit> CreateStarted { get; } =
            new TaskCompletionSource<Unit>(TaskCreationOptions.RunContinuationsAsynchronously);

        public async ValueTask<Result<RuntimeEntityHandle, SpawnError>> CreateAsync(PlayerEntityCreateRequest request, CancellationToken token)
        {
            CreateCalls++;
            CreateStarted.TrySetResult(Unit.Value);
            if (ThrowOnCreateWhenCancelled && token.IsCancellationRequested)
                throw new OperationCanceledException(token);
            if (BlockCreate)
            {
                var cancelled = new TaskCompletionSource<Result<RuntimeEntityHandle, SpawnError>>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                using (token.Register(() => cancelled.TrySetCanceled(token)))
                    return await cancelled.Task;
            }
            if (CreateResults.Count > 0) return CreateResults.Dequeue();
            _created++;
            return Result<RuntimeEntityHandle, SpawnError>.Success(
                new RuntimeEntityHandle(new Guid(_created, 0, 0, new byte[8])));
        }

        public ValueTask<Result<Unit, SpawnError>> DestroyAsync(RuntimeEntityHandle handle, CancellationToken token)
        {
            DestroyCalls++;
            return new ValueTask<Result<Unit, SpawnError>>(DestroyResult);
        }
    }

    internal sealed class FakeSpawnTelemetry : ISpawnTelemetry
    {
        public List<SpawnTelemetryEvent> Events { get; } = new List<SpawnTelemetryEvent>();

        public void Record(SpawnTelemetryEvent value)
        {
            Events.Add(value);
        }
    }
}
