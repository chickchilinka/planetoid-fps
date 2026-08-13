using System;
using Modules.Multiplayer.Primitives;

namespace Modules.Multiplayer.Spawning
{
    public readonly struct RuntimeEntityHandle : IEquatable<RuntimeEntityHandle>
    {
        public RuntimeEntityHandle(Guid value)
        {
            if (value == Guid.Empty) throw new ArgumentException("Runtime entity handle must be valid.", nameof(value));
            Value = value;
        }

        public Guid Value { get; }
        public bool Equals(RuntimeEntityHandle other) => Value.Equals(other.Value);
        public override bool Equals(object obj) => obj is RuntimeEntityHandle other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public static bool operator ==(RuntimeEntityHandle left, RuntimeEntityHandle right) => left.Equals(right);
        public static bool operator !=(RuntimeEntityHandle left, RuntimeEntityHandle right) => !left.Equals(right);
    }

    public readonly struct SpawnReservation : IEquatable<SpawnReservation>
    {
        public SpawnReservation(string pointId, SpawnPose pose)
        {
            if (string.IsNullOrWhiteSpace(pointId)) throw new ArgumentException("Spawn point ID is required.", nameof(pointId));
            PointId = pointId;
            Pose = pose;
        }

        public string PointId { get; }
        public SpawnPose Pose { get; }
        public bool Equals(SpawnReservation other) => string.Equals(PointId, other.PointId, StringComparison.Ordinal) && Pose.Equals(other.Pose);
        public override bool Equals(object obj) => obj is SpawnReservation other && Equals(other);
        public override int GetHashCode()
        {
            unchecked
            {
                return ((PointId != null ? StringComparer.Ordinal.GetHashCode(PointId) : 0) * 397) ^ Pose.GetHashCode();
            }
        }
        public static bool operator ==(SpawnReservation left, SpawnReservation right) => left.Equals(right);
        public static bool operator !=(SpawnReservation left, SpawnReservation right) => !left.Equals(right);
    }

    public readonly struct SpawnHandle : IEquatable<SpawnHandle>
    {
        public SpawnHandle(PlayerId playerId, MatchId matchId, RuntimeEntityHandle runtimeEntity, SpawnReservation reservation)
        {
            if (!playerId.IsValid) throw new ArgumentException("Player ID must be valid.", nameof(playerId));
            if (!matchId.IsValid) throw new ArgumentException("Match ID must be valid.", nameof(matchId));
            PlayerId = playerId;
            MatchId = matchId;
            RuntimeEntity = runtimeEntity;
            Reservation = reservation;
        }

        public PlayerId PlayerId { get; }
        public MatchId MatchId { get; }
        public RuntimeEntityHandle RuntimeEntity { get; }
        public SpawnReservation Reservation { get; }
        public bool Equals(SpawnHandle other) => PlayerId.Equals(other.PlayerId) && MatchId.Equals(other.MatchId)
            && RuntimeEntity.Equals(other.RuntimeEntity) && Reservation.Equals(other.Reservation);
        public override bool Equals(object obj) => obj is SpawnHandle other && Equals(other);
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = PlayerId.GetHashCode();
                hashCode = (hashCode * 397) ^ MatchId.GetHashCode();
                hashCode = (hashCode * 397) ^ RuntimeEntity.GetHashCode();
                return (hashCode * 397) ^ Reservation.GetHashCode();
            }
        }
        public static bool operator ==(SpawnHandle left, SpawnHandle right) => left.Equals(right);
        public static bool operator !=(SpawnHandle left, SpawnHandle right) => !left.Equals(right);
    }
}
