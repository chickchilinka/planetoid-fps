using System;
using Modules.Multiplayer.Primitives;

namespace Modules.Multiplayer.Spawning
{
    public readonly struct PlayerSpawnRequest : IEquatable<PlayerSpawnRequest>
    {
        public PlayerSpawnRequest(PlayerId playerId, MatchId matchId)
        {
            if (!playerId.IsValid) throw new ArgumentException("Player ID must be valid.", nameof(playerId));
            if (!matchId.IsValid) throw new ArgumentException("Match ID must be valid.", nameof(matchId));
            PlayerId = playerId;
            MatchId = matchId;
        }

        public PlayerId PlayerId { get; }
        public MatchId MatchId { get; }
        public bool Equals(PlayerSpawnRequest other) => PlayerId.Equals(other.PlayerId) && MatchId.Equals(other.MatchId);
        public override bool Equals(object obj) => obj is PlayerSpawnRequest other && Equals(other);
        public override int GetHashCode()
        {
            unchecked { return (PlayerId.GetHashCode() * 397) ^ MatchId.GetHashCode(); }
        }
        public static bool operator ==(PlayerSpawnRequest left, PlayerSpawnRequest right) => left.Equals(right);
        public static bool operator !=(PlayerSpawnRequest left, PlayerSpawnRequest right) => !left.Equals(right);
    }

    public readonly struct PlayerEntityCreateRequest : IEquatable<PlayerEntityCreateRequest>
    {
        public PlayerEntityCreateRequest(PlayerSpawnRequest spawn, SpawnPose pose)
        {
            Spawn = spawn;
            Pose = pose;
        }

        public PlayerSpawnRequest Spawn { get; }
        public PlayerId PlayerId => Spawn.PlayerId;
        public MatchId MatchId => Spawn.MatchId;
        public SpawnPose Pose { get; }
        public bool Equals(PlayerEntityCreateRequest other) => Spawn.Equals(other.Spawn) && Pose.Equals(other.Pose);
        public override bool Equals(object obj) => obj is PlayerEntityCreateRequest other && Equals(other);
        public override int GetHashCode()
        {
            unchecked { return (Spawn.GetHashCode() * 397) ^ Pose.GetHashCode(); }
        }
        public static bool operator ==(PlayerEntityCreateRequest left, PlayerEntityCreateRequest right) => left.Equals(right);
        public static bool operator !=(PlayerEntityCreateRequest left, PlayerEntityCreateRequest right) => !left.Equals(right);
    }
}
