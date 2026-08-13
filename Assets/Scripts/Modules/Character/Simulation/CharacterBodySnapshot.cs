using System;
using UnityEngine;

namespace Modules.Character.Simulation
{
    public readonly struct CharacterBodySnapshot : IEquatable<CharacterBodySnapshot>
    {
        public CharacterBodySnapshot(
            Vector3 position,
            Quaternion rotation,
            Vector3 linearVelocity,
            bool isGrounded,
            Vector3 groundNormal)
        {
            Position = position;
            Rotation = rotation;
            LinearVelocity = linearVelocity;
            IsGrounded = isGrounded;
            GroundNormal = groundNormal;
        }

        public Vector3 Position { get; }

        public Quaternion Rotation { get; }

        public Vector3 LinearVelocity { get; }

        public bool IsGrounded { get; }

        public Vector3 GroundNormal { get; }

        public bool Equals(CharacterBodySnapshot other)
        {
            return Position.Equals(other.Position) &&
                   Rotation.Equals(other.Rotation) &&
                   LinearVelocity.Equals(other.LinearVelocity) &&
                   IsGrounded == other.IsGrounded &&
                   GroundNormal.Equals(other.GroundNormal);
        }

        public override bool Equals(object obj)
        {
            return obj is CharacterBodySnapshot other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Position.GetHashCode();
                hashCode = (hashCode * 397) ^ Rotation.GetHashCode();
                hashCode = (hashCode * 397) ^ LinearVelocity.GetHashCode();
                hashCode = (hashCode * 397) ^ IsGrounded.GetHashCode();
                hashCode = (hashCode * 397) ^ GroundNormal.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(CharacterBodySnapshot left, CharacterBodySnapshot right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CharacterBodySnapshot left, CharacterBodySnapshot right)
        {
            return !left.Equals(right);
        }
    }
}
