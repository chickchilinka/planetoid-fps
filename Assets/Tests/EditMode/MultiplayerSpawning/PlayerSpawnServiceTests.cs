using System;
using System.Threading;
using System.Threading.Tasks;
using Modules.Multiplayer.Primitives;
using NUnit.Framework;

namespace Modules.Multiplayer.Spawning.Tests
{
    public sealed class PlayerSpawnServiceTests
    {
        [Test]
        public async Task DuplicateSpawn_ReturnsExistingHandleWithoutSecondCreation()
        {
            var fixture = SpawnFixture.Successful();

            var first = await fixture.Service.SpawnAsync(fixture.Request, default);
            var second = await fixture.Service.SpawnAsync(fixture.Request, default);

            Assert.That(second.Value, Is.EqualTo(first.Value));
            Assert.That(fixture.Runtime.CreateCalls, Is.EqualTo(1));
        }

        [Test]
        public async Task FirstCreationFailure_RetriesAtDifferentPoint()
        {
            var fixture = SpawnFixture.FailFirstCreation();

            var result = await fixture.Service.SpawnAsync(fixture.Request, default);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(fixture.Points.ReservedIds, Is.EqualTo(new[] { "spawn-a", "spawn-b" }));
            Assert.That(fixture.Points.ReleasedIds, Does.Contain("spawn-a"));
        }

        [Test]
        public async Task CreationFailureAfterRetries_ReleasesEveryReservation()
        {
            var fixture = SpawnFixture.Successful();
            fixture.Runtime.CreateResults.Enqueue(Result<RuntimeEntityHandle, SpawnError>.Failure(SpawnError.EntityCreationFailed));
            fixture.Runtime.CreateResults.Enqueue(Result<RuntimeEntityHandle, SpawnError>.Failure(SpawnError.EntityCreationFailed));

            var result = await fixture.Service.SpawnAsync(fixture.Request, default);

            Assert.That(result.Error, Is.EqualTo(SpawnError.EntityCreationFailed));
            Assert.That(fixture.Points.ReleasedIds, Is.EqualTo(new[] { "spawn-a", "spawn-b" }));
        }

        [Test]
        public async Task CancelledCreation_ReleasesReservationAndReturnsCancelled()
        {
            var fixture = SpawnFixture.Successful();
            fixture.Runtime.BlockCreate = true;
            using var cancellation = new CancellationTokenSource();
            var pending = fixture.Service.SpawnAsync(fixture.Request, cancellation.Token).AsTask();
            await fixture.Runtime.CreateStarted.Task;
            cancellation.Cancel();

            var result = await pending;

            Assert.That(result.Error, Is.EqualTo(SpawnError.Cancelled));
            Assert.That(fixture.Points.ReleasedIds, Is.EqualTo(new[] { "spawn-a" }));
        }

        [Test]
        public async Task DespawnMissing_ReturnsAlreadyAbsent()
        {
            var result = await SpawnFixture.Successful().Service.DespawnAsync(
                SpawnFixture.Player("40000000-0000-0000-0000-000000000001"), default);

            Assert.That(result.Value, Is.EqualTo(DespawnOutcome.AlreadyAbsent));
        }

        [Test]
        public async Task DestroyFailure_KeepsRegistryAndReservationForRetry()
        {
            var fixture = SpawnFixture.Successful();
            var spawned = await fixture.Service.SpawnAsync(fixture.Request, default);
            fixture.Runtime.DestroyResult = Result<Unit, SpawnError>.Failure(SpawnError.WorldNotReady);

            var first = await fixture.Service.DespawnAsync(fixture.Request.PlayerId, default);
            fixture.Runtime.DestroyResult = Result<Unit, SpawnError>.Success(Unit.Value);
            var second = await fixture.Service.DespawnAsync(fixture.Request.PlayerId, default);

            Assert.That(first.Error, Is.EqualTo(SpawnError.WorldNotReady));
            Assert.That(second.Value, Is.EqualTo(DespawnOutcome.Despawned));
            Assert.That(fixture.Runtime.DestroyCalls, Is.EqualTo(2));
            Assert.That(fixture.Points.ReleasedIds, Does.Contain(spawned.Value.Reservation.PointId));
        }
    }
}
