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

        [Test]
        public async Task LoadProvider_CanCompleteWholeInitialPipelineBeforeReturning()
        {
            var fixture = SessionFixture.Create(
                worldCallsAllReady: true,
                worldLoadTimeout: TimeSpan.FromMilliseconds(40));
            var first = (await fixture.JoinAsync()).Value;
            var second = (await fixture.JoinAsync()).Value;
            await fixture.Service.SetReadyAsync(first, true, default);

            var completion = fixture.Service.SetReadyAsync(second, true, default).AsTask();

            Assert.That(await CompletesWithinAsync(completion), Is.True);
            Assert.That((await completion).IsSuccess, Is.True);
            Assert.That(fixture.Service.Snapshot.Phase, Is.EqualTo(SessionPhase.Playing));
            await Task.Delay(80);
            Assert.That(fixture.World.CancelledOperations, Is.Empty);
        }

        [Test]
        public async Task ReadySignals_SpawnEachInitialPlayerExactlyOnce_ThenEnterPlaying()
        {
            var fixture = SessionFixture.Create();
            var (first, second, load) = await StartLoadingAsync(fixture);

            await fixture.Service.NotifyPlayerWorldReadyAsync(load.OperationId, first, load.MatchId, default);
            await fixture.Service.NotifyPlayerWorldReadyAsync(load.OperationId, second, load.MatchId, default);
            await fixture.Service.NotifyServerWorldReadyAsync(load.OperationId, load.MatchId, default);
            await fixture.Service.NotifyServerWorldReadyAsync(load.OperationId, load.MatchId, default);
            await fixture.Service.NotifyPlayerWorldReadyAsync(load.OperationId, first, load.MatchId, default);

            Assert.That(fixture.Spawn.SpawnRequests.Select(x => x.PlayerId),
                Is.EquivalentTo(new[] { first, second }));
            Assert.That(fixture.Spawn.SpawnRequests, Has.Count.EqualTo(2));
            Assert.That(fixture.Service.Snapshot.Player(first).SpawnState, Is.EqualTo(SpawnState.Spawned));
            Assert.That(fixture.Service.Snapshot.Player(second).SpawnState, Is.EqualTo(SpawnState.Spawned));
            Assert.That(fixture.Service.Snapshot.Phase, Is.EqualTo(SessionPhase.Playing));
        }

        [Test]
        public async Task JoinDuringInitialLoad_IsJipAndDoesNotBlockPlayingTransition()
        {
            var fixture = SessionFixture.Create();
            var (first, second, load) = await StartLoadingAsync(fixture);
            var jip = (await fixture.JoinAsync()).Value;

            await fixture.Service.NotifyPlayerWorldReadyAsync(load.OperationId, first, load.MatchId, default);
            await fixture.Service.NotifyPlayerWorldReadyAsync(load.OperationId, second, load.MatchId, default);
            await fixture.Service.NotifyServerWorldReadyAsync(load.OperationId, load.MatchId, default);

            Assert.That(fixture.Service.Snapshot.Phase, Is.EqualTo(SessionPhase.Playing));
            Assert.That(fixture.Service.Snapshot.Player(jip).JoinKind, Is.EqualTo(SessionJoinKind.InProgress));
            Assert.That(fixture.Service.Snapshot.Player(jip).SpawnState, Is.EqualTo(SpawnState.NotSpawned));
            Assert.That(fixture.Spawn.SpawnRequests.Any(x => x.PlayerId == jip), Is.False);
        }

        [Test]
        public async Task JoinDuringPlaying_UsesReadinessAndSpawnPipeline()
        {
            var fixture = SessionFixture.Create();
            var load = await AdvanceToPlayingAsync(fixture);
            var jip = (await fixture.JoinAsync()).Value;

            await fixture.Service.NotifyPlayerWorldReadyAsync(load.OperationId, jip, load.MatchId, default);

            Assert.That(fixture.Service.Snapshot.Phase, Is.EqualTo(SessionPhase.Playing));
            Assert.That(fixture.Service.Snapshot.Player(jip).JoinKind, Is.EqualTo(SessionJoinKind.InProgress));
            Assert.That(fixture.Service.Snapshot.Player(jip).SpawnState, Is.EqualTo(SpawnState.Spawned));
            Assert.That(fixture.Spawn.SpawnRequests.Count(x => x.PlayerId == jip), Is.EqualTo(1));
        }

        [Test]
        public async Task StaleWorldReady_DoesNotAdvanceReplacementOperation()
        {
            var fixture = SessionFixture.Create();
            var (first, second, staleLoad) = await StartLoadingAsync(fixture);
            await fixture.Service.LeaveAsync(second, default);
            var replacement = (await fixture.JoinAsync()).Value;
            await fixture.Service.SetReadyAsync(first, true, default);
            await fixture.Service.SetReadyAsync(replacement, true, default);
            var replacementLoad = fixture.World.LoadRequests.Last();

            var stale = await fixture.Service.NotifyServerWorldReadyAsync(
                staleLoad.OperationId, staleLoad.MatchId, default);

            Assert.That(stale.IsFailure, Is.True);
            Assert.That(stale.Error, Is.EqualTo(SessionError.InvalidPhase));
            Assert.That(replacementLoad.OperationId, Is.Not.EqualTo(staleLoad.OperationId));
            Assert.That(fixture.Service.Snapshot.ServerWorldReady, Is.False);
        }

        [Test]
        public async Task LoadFailure_CancelsOperationAndResetsLobbyState()
        {
            var fixture = SessionFixture.Create();
            fixture.World.LoadResult = Result<Unit, SessionError>.Failure(SessionError.WorldLoadFailed);
            var first = (await fixture.JoinAsync()).Value;
            var second = (await fixture.JoinAsync()).Value;
            await fixture.Service.SetReadyAsync(first, true, default);

            var result = await fixture.Service.SetReadyAsync(second, true, default);

            Assert.That(result.Error, Is.EqualTo(SessionError.WorldLoadFailed));
            Assert.That(fixture.Service.Snapshot.Phase, Is.EqualTo(SessionPhase.Lobby));
            Assert.That(fixture.Service.Snapshot.MatchId, Is.EqualTo(MatchId.None));
            Assert.That(fixture.Service.Snapshot.Players.All(x => !x.Ready && !x.WorldReady), Is.True);
            Assert.That(fixture.World.CancelledOperations, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task LoadTimeout_RecoversEvenWhenProviderIgnoresCancellation()
        {
            var fixture = SessionFixture.Create(worldLoadTimeout: TimeSpan.FromMilliseconds(40));
            fixture.World.BlockLoad = true;
            fixture.World.IgnoreCancellation = true;
            var first = (await fixture.JoinAsync()).Value;
            var second = (await fixture.JoinAsync()).Value;
            await fixture.Service.SetReadyAsync(first, true, default);

            var result = await fixture.Service.SetReadyAsync(second, true, default);

            Assert.That(result.Error, Is.EqualTo(SessionError.WorldLoadTimeout));
            Assert.That(fixture.Service.Snapshot.Phase, Is.EqualTo(SessionPhase.Lobby));
            Assert.That(fixture.Service.Snapshot.Players.All(x => !x.Ready), Is.True);
            Assert.That(fixture.World.CancelledOperations, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task SuccessfulLoadReturn_DoesNotDisarmServerReadinessTimeout()
        {
            var fixture = SessionFixture.Create(worldLoadTimeout: TimeSpan.FromMilliseconds(40));
            var first = (await fixture.JoinAsync()).Value;
            var second = (await fixture.JoinAsync()).Value;
            await fixture.Service.SetReadyAsync(first, true, default);

            var result = await fixture.Service.SetReadyAsync(second, true, default);
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(await WaitUntilAsync(
                () => fixture.Service.Snapshot.Phase == SessionPhase.Lobby), Is.True);

            Assert.That(fixture.Service.Snapshot.Players.All(x => !x.Ready), Is.True);
            Assert.That(fixture.World.CancelledOperations, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task ServerReadyWithoutAllInitialPlayers_StillTimesOutInitialLoad()
        {
            var fixture = SessionFixture.Create(worldLoadTimeout: TimeSpan.FromMilliseconds(40));
            var (first, _, load) = await StartLoadingAsync(fixture);
            await fixture.Service.NotifyPlayerWorldReadyAsync(load.OperationId, first, load.MatchId, default);
            await fixture.Service.NotifyServerWorldReadyAsync(load.OperationId, load.MatchId, default);

            Assert.That(await WaitUntilAsync(
                () => fixture.Service.Snapshot.Phase == SessionPhase.Lobby), Is.True);

            Assert.That(fixture.Service.Snapshot.ServerWorldReady, Is.False);
            Assert.That(fixture.Service.Snapshot.Players.All(
                x => !x.Ready && !x.WorldReady && x.SpawnState == SpawnState.NotSpawned), Is.True);
            Assert.That(fixture.World.CancelledOperations, Has.Count.EqualTo(1));
            Assert.That(fixture.Spawn.DespawnRequests, Is.EqualTo(new[] { first }));
        }

        [Test]
        public async Task DisconnectDuringInitialLoadBelowTwo_CancelsAndResetsReady()
        {
            var fixture = SessionFixture.Create();
            var (_, second, load) = await StartLoadingAsync(fixture);

            await fixture.Service.LeaveAsync(second, default);

            Assert.That(fixture.World.CancelledOperations, Is.EqualTo(new[] { load.OperationId }));
            Assert.That(fixture.Service.Snapshot.Phase, Is.EqualTo(SessionPhase.WaitingForPlayers));
            Assert.That(fixture.Service.Snapshot.Players.All(x => !x.Ready), Is.True);
        }

        [Test]
        public async Task DisconnectDuringPlaying_DespawnsOnlyThatPlayerAndKeepsPlaying()
        {
            var fixture = SessionFixture.Create();
            await AdvanceToPlayingAsync(fixture);
            var leaving = fixture.Service.Snapshot.Players[0].PlayerId;
            var remaining = fixture.Service.Snapshot.Players[1].PlayerId;

            await fixture.Service.LeaveAsync(leaving, default);

            Assert.That(fixture.Service.Snapshot.Phase, Is.EqualTo(SessionPhase.Playing));
            Assert.That(fixture.Service.Snapshot.Players.Select(x => x.PlayerId), Is.EqualTo(new[] { remaining }));
            Assert.That(fixture.Spawn.DespawnRequests, Is.EqualTo(new[] { leaving }));
            Assert.That(fixture.World.CancelledOperations, Is.Empty);
        }

        [Test]
        public async Task JipLoadTimeout_DisconnectsOnlyJipAndDoesNotStopMatch()
        {
            var fixture = SessionFixture.Create(playerLoadTimeout: TimeSpan.FromMilliseconds(40));
            await AdvanceToPlayingAsync(fixture);
            var jip = (await fixture.JoinAsync()).Value;

            Assert.That(await WaitUntilAsync(() => fixture.Connections.Disconnects.Count == 1), Is.True);

            var disconnected = fixture.Connections.Disconnects.Single();
            Assert.That(disconnected.Connection, Is.EqualTo(fixture.PlayerConnections[jip]));
            Assert.That(disconnected.Reason, Is.EqualTo(SessionError.PlayerLoadTimeout));
            Assert.That(fixture.Service.Snapshot.Phase, Is.EqualTo(SessionPhase.Playing));
            Assert.That(fixture.World.CancelledOperations, Is.Empty);
        }

        [Test]
        public async Task SpawnCompletionFromCancelledOperation_CannotMutateReplacementState()
        {
            var fixture = SessionFixture.Create();
            fixture.Spawn.BlockSpawn = true;
            fixture.Spawn.IgnoreCancellation = true;
            var (first, second, staleLoad) = await StartLoadingAsync(fixture);
            await fixture.Service.NotifyPlayerWorldReadyAsync(staleLoad.OperationId, first, staleLoad.MatchId, default);
            var spawn = fixture.Service.NotifyServerWorldReadyAsync(
                staleLoad.OperationId, staleLoad.MatchId, default).AsTask();
            Assert.That(await WaitUntilAsync(() => fixture.Spawn.SpawnRequests.Count == 1), Is.True);

            await fixture.Service.LeaveAsync(second, default);
            var replacement = (await fixture.JoinAsync()).Value;
            await fixture.Service.SetReadyAsync(first, true, default);
            await fixture.Service.SetReadyAsync(replacement, true, default);
            var replacementLoad = fixture.World.LoadRequests.Last();
            fixture.Spawn.SpawnCompletion.TrySetResult(
                Result<SpawnPlayerResult, SessionError>.Success(SpawnPlayerResult.Completed));
            await spawn;

            Assert.That(replacementLoad.OperationId, Is.Not.EqualTo(staleLoad.OperationId));
            Assert.That(fixture.Service.Snapshot.Phase, Is.EqualTo(SessionPhase.LoadingMatch));
            Assert.That(fixture.Service.Snapshot.ServerWorldReady, Is.False);
            Assert.That(fixture.Service.Snapshot.Player(first).SpawnState, Is.EqualTo(SpawnState.NotSpawned));
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

        private static async Task<(PlayerId First, PlayerId Second, MatchWorldLoadRequest Load)>
            StartLoadingAsync(SessionFixture fixture)
        {
            var first = (await fixture.JoinAsync()).Value;
            var second = (await fixture.JoinAsync()).Value;
            await fixture.Service.SetReadyAsync(first, true, default);
            await fixture.Service.SetReadyAsync(second, true, default);
            return (first, second, fixture.World.LoadRequests.Last());
        }

        private static async Task<MatchWorldLoadRequest> AdvanceToPlayingAsync(SessionFixture fixture)
        {
            var (first, second, load) = await StartLoadingAsync(fixture);
            await fixture.Service.NotifyPlayerWorldReadyAsync(load.OperationId, first, load.MatchId, default);
            await fixture.Service.NotifyPlayerWorldReadyAsync(load.OperationId, second, load.MatchId, default);
            await fixture.Service.NotifyServerWorldReadyAsync(load.OperationId, load.MatchId, default);
            Assert.That(fixture.Service.Snapshot.Phase, Is.EqualTo(SessionPhase.Playing));
            return load;
        }

        private static async Task<bool> WaitUntilAsync(Func<bool> condition)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
            while (DateTime.UtcNow < deadline)
            {
                if (condition()) return true;
                await Task.Delay(10);
            }

            return condition();
        }
    }
}
