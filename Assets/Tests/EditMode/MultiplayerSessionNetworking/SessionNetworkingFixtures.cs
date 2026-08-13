using System;
using Modules.Multiplayer.Primitives;
using Modules.Multiplayer.Session;

namespace Modules.Multiplayer.Session.Networking.Tests
{
    internal static class SessionNetworkingFixtures
    {
        public static SessionSnapshot PlayingWithThreePlayers(long revision = 17)
        {
            return new SessionSnapshot(
                Session("10000000-0000-0000-0000-000000000001"),
                Match("20000000-0000-0000-0000-000000000002"),
                Map("30000000-0000-0000-0000-000000000003"),
                SessionPhase.Playing,
                revision,
                true,
                new[]
                {
                    new SessionPlayerSnapshot(
                        Player("40000000-0000-0000-0000-000000000004"),
                        true,
                        true,
                        SpawnState.Spawned,
                        SessionJoinKind.Initial),
                    new SessionPlayerSnapshot(
                        Player("50000000-0000-0000-0000-000000000005"),
                        false,
                        true,
                        SpawnState.Spawning,
                        SessionJoinKind.InProgress),
                    new SessionPlayerSnapshot(
                        Player("60000000-0000-0000-0000-000000000006"),
                        false,
                        false,
                        SpawnState.NotSpawned,
                        SessionJoinKind.InProgress)
                });
        }

        public static SessionId Session(string value) => new SessionId(Guid.Parse(value));
        public static MatchId Match(string value) => new MatchId(Guid.Parse(value));
        public static MapId Map(string value) => new MapId(Guid.Parse(value));
        public static PlayerId Player(string value) => new PlayerId(Guid.Parse(value));
    }
}
