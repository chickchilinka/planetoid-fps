using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Modules.Multiplayer.Primitives;

namespace Modules.Multiplayer.Spawning
{
    public sealed class PlayerSpawnService : IPlayerSpawnService, IDisposable
    {
        private readonly ISpawnPointProvider _points;
        private readonly IPlayerEntityRuntimeProvider _runtime;
        private readonly ISpawnTelemetry _telemetry;
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
        private readonly Dictionary<PlayerId, SpawnHandle> _spawned = new Dictionary<PlayerId, SpawnHandle>();
        private bool _disposed;

        public PlayerSpawnService(ISpawnPointProvider points, IPlayerEntityRuntimeProvider runtime, ISpawnTelemetry telemetry)
        {
            _points = points ?? throw new ArgumentNullException(nameof(points));
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        }

        public async ValueTask<Result<SpawnHandle, SpawnError>> SpawnAsync(PlayerSpawnRequest request, CancellationToken token)
        {
            var entered = false;
            SpawnReservation? heldReservation = null;
            var stopwatch = Stopwatch.StartNew();
            try
            {
                ThrowIfDisposed();
                await _gate.WaitAsync(token);
                entered = true;
                if (_spawned.TryGetValue(request.PlayerId, out var existing))
                    return Result<SpawnHandle, SpawnError>.Success(existing);

                var excluded = new HashSet<string>(StringComparer.Ordinal);
                for (var attempt = 1; attempt <= 2; attempt++)
                {
                    token.ThrowIfCancellationRequested();
                    var reserved = _points.Reserve(request.PlayerId, excluded);
                    if (reserved.IsFailure) return Result<SpawnHandle, SpawnError>.Failure(reserved.Error);

                    heldReservation = reserved.Value;
                    excluded.Add(heldReservation.Value.PointId);
                    Record(SpawnTelemetryKind.Reserved, request, heldReservation.Value, attempt, stopwatch.Elapsed);
                    var created = await _runtime.CreateAsync(new PlayerEntityCreateRequest(request, heldReservation.Value.Pose), token);
                    if (created.IsSuccess)
                    {
                        var handle = new SpawnHandle(request.PlayerId, request.MatchId, created.Value, heldReservation.Value);
                        _spawned.Add(request.PlayerId, handle);
                        heldReservation = null;
                        Record(SpawnTelemetryKind.Spawned, request, handle.Reservation, attempt, stopwatch.Elapsed);
                        return Result<SpawnHandle, SpawnError>.Success(handle);
                    }

                    Record(SpawnTelemetryKind.AttemptFailed, request, heldReservation.Value, attempt, stopwatch.Elapsed, created.Error);
                    Release(request, heldReservation.Value, attempt, stopwatch.Elapsed, created.Error);
                    heldReservation = null;
                    if (created.Error != SpawnError.EntityCreationFailed)
                        return Result<SpawnHandle, SpawnError>.Failure(created.Error);
                }

                return Result<SpawnHandle, SpawnError>.Failure(SpawnError.EntityCreationFailed);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                if (heldReservation.HasValue)
                    Release(request, heldReservation.Value, 0, stopwatch.Elapsed, SpawnError.Cancelled);
                return Result<SpawnHandle, SpawnError>.Failure(SpawnError.Cancelled);
            }
            finally
            {
                if (entered) _gate.Release();
            }
        }

        public async ValueTask<Result<DespawnOutcome, SpawnError>> DespawnAsync(PlayerId playerId, CancellationToken token)
        {
            var entered = false;
            var stopwatch = Stopwatch.StartNew();
            try
            {
                ThrowIfDisposed();
                await _gate.WaitAsync(token);
                entered = true;
                if (!_spawned.TryGetValue(playerId, out var handle))
                    return Result<DespawnOutcome, SpawnError>.Success(DespawnOutcome.AlreadyAbsent);

                var destroyed = await _runtime.DestroyAsync(handle.RuntimeEntity, token);
                if (destroyed.IsFailure) return Result<DespawnOutcome, SpawnError>.Failure(destroyed.Error);

                _spawned.Remove(playerId);
                Record(SpawnTelemetryKind.Destroyed, new PlayerSpawnRequest(handle.PlayerId, handle.MatchId), handle.Reservation, 1, stopwatch.Elapsed);
                Release(new PlayerSpawnRequest(handle.PlayerId, handle.MatchId), handle.Reservation, 1, stopwatch.Elapsed, null);
                return Result<DespawnOutcome, SpawnError>.Success(DespawnOutcome.Despawned);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                return Result<DespawnOutcome, SpawnError>.Failure(SpawnError.Cancelled);
            }
            finally
            {
                if (entered) _gate.Release();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _gate.Dispose();
        }

        private void Release(PlayerSpawnRequest request, SpawnReservation reservation, int attempt, TimeSpan duration, SpawnError? error)
        {
            _points.Release(reservation);
            Record(SpawnTelemetryKind.Released, request, reservation, attempt, duration, error);
        }

        private void Record(SpawnTelemetryKind kind, PlayerSpawnRequest request, SpawnReservation reservation, int attempt, TimeSpan duration, SpawnError? error = null)
        {
            _telemetry.Record(new SpawnTelemetryEvent(kind, request.PlayerId, request.MatchId, reservation.PointId, attempt, duration, error));
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(PlayerSpawnService));
        }
    }
}
