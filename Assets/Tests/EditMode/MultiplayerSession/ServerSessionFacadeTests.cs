using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Modules.Multiplayer.Primitives;
using NUnit.Framework;

namespace Modules.Multiplayer.Session.Tests
{
    public sealed class ServerSessionFacadeTests
    {
        [Test]
        public async Task TwoPlayers_AllReady_CommitsOneMatchLoad()
        {
            var fixture = SessionFixture.Create();
            var first = (await fixture.JoinAsync()).Value;
            var second = (await fixture.JoinAsync()).Value;

            await fixture.Service.SetReadyAsync(first, true, default);
            await fixture.Service.SetReadyAsync(second, true, default);

            Assert.That(fixture.Service.Snapshot.Phase, Is.EqualTo(SessionPhase.LoadingMatch));
            Assert.That(fixture.World.LoadRequests, Has.Count.EqualTo(1));
            Assert.That(fixture.Service.Snapshot.MatchId.IsValid, Is.True);
            Assert.That(fixture.World.LoadRequests[0].OperationId.IsValid, Is.True);
        }

        [Test]
        public async Task ReadyRepeated_DoesNotIncrementRevisionOrStartTwice()
        {
            var fixture = SessionFixture.Create();
            var first = (await fixture.JoinAsync()).Value;
            await fixture.JoinAsync();
            var before = fixture.Service.Snapshot.Revision;

            await fixture.Service.SetReadyAsync(first, true, default);
            var changed = fixture.Service.Snapshot.Revision;
            await fixture.Service.SetReadyAsync(first, true, default);

            Assert.That(changed, Is.EqualTo(before + 1));
            Assert.That(fixture.Service.Snapshot.Revision, Is.EqualTo(changed));
            Assert.That(fixture.World.LoadRequests, Is.Empty);
        }

        [Test]
        public async Task EleventhJoin_ReturnsSessionFull()
        {
            var fixture = SessionFixture.Create();
            for (var i = 0; i < 10; i++)
                Assert.That((await fixture.JoinAsync()).IsSuccess, Is.True);

            var result = await fixture.JoinAsync();

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error, Is.EqualTo(SessionError.SessionFull));
        }

        [Test]
        public async Task Snapshots_AreImmutableOrderedAndPublishedInRevisionOrder()
        {
            var fixture = SessionFixture.Create();
            for (var i = 0; i < 4; i++) await fixture.JoinAsync();
            var captured = fixture.Publisher.Snapshots[1];
            var capturedIds = captured.Players.Select(player => player.PlayerId.Value).ToArray();

            await fixture.JoinAsync();

            Assert.That(captured.Players, Has.Count.EqualTo(2));
            Assert.That(captured.Players.Select(player => player.PlayerId.Value), Is.EqualTo(capturedIds));
            Assert.That(fixture.Publisher.Snapshots.Select(snapshot => snapshot.Revision), Is.Ordered);
            Assert.That(fixture.Service.Snapshot.Players.Select(player => player.PlayerId.Value), Is.Ordered);
        }

        [Test]
        public async Task LoadProvider_CanCallBackWithoutDeadlockingAndRequiresMatchingIds()
        {
            var fixture = SessionFixture.Create(worldCallsReady: true);
            var first = (await fixture.JoinAsync()).Value;
            var second = (await fixture.JoinAsync()).Value;
            await fixture.Service.SetReadyAsync(first, true, default);

            var completion = fixture.Service.SetReadyAsync(second, true, default).AsTask();
            Assert.That(await CompletesWithinAsync(completion), Is.True);
            Assert.That(fixture.Service.Snapshot.ServerWorldReady, Is.True);

            var stale = await fixture.Service.NotifyPlayerWorldReadyAsync(
                new OperationId(Guid.NewGuid()), first, fixture.Service.Snapshot.MatchId, default);
            Assert.That(stale.Error, Is.EqualTo(SessionError.InvalidPhase));
        }

        [TestCase(9, 2)]
        [TestCase(10, 1)]
        [TestCase(11, 2)]
        public void Configuration_RejectsUnsupportedCapacity(int maxPlayers, int minPlayers)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SessionConfiguration(
                maxPlayers, minPlayers, new MapId(Guid.NewGuid()), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10)));
        }

        [Test]
        public void Configuration_RejectsInvalidMapAndNonPositiveTimeouts()
        {
            var mapId = new MapId(Guid.NewGuid());
            Assert.Throws<ArgumentException>(() => new SessionConfiguration(
                10, 2, MapId.None, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SessionConfiguration(
                10, 2, mapId, TimeSpan.Zero, TimeSpan.FromSeconds(10)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SessionConfiguration(
                10, 2, mapId, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(-1)));
        }

        private static async Task<bool> CompletesWithinAsync(Task task)
        {
            var winner = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(2)));
            return ReferenceEquals(winner, task);
        }
    }
}
