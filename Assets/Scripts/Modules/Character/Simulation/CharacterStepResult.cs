using System;
using UnityEngine;

namespace Modules.Character.Simulation
{
    public readonly struct CharacterStepResult : IEquatable<CharacterStepResult>
    {
        public CharacterStepResult(
            Vector3 linearVelocity,
            Vector3 acceleration,
            Quaternion targetRotation,
            CharacterSimulationState state)
        {
            LinearVelocity = linearVelocity;
            Acceleration = acceleration;
            TargetRotation = targetRotation;
            State = state;
        }

        public Vector3 LinearVelocity { get; }

        public Vector3 Acceleration { get; }

        public Quaternion TargetRotation { get; }

        public CharacterSimulationState State { get; }

        public bool Equals(CharacterStepResult other)
        {
            return LinearVelocity.Equals(other.LinearVelocity) &&
                   Acceleration.Equals(other.Acceleration) &&
                   TargetRotation.Equals(other.TargetRotation) &&
                   State.Equals(other.State);
        }

        public override bool Equals(object obj)
        {
            return obj is CharacterStepResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = LinearVelocity.GetHashCode();
                hashCode = (hashCode * 397) ^ Acceleration.GetHashCode();
                hashCode = (hashCode * 397) ^ TargetRotation.GetHashCode();
                hashCode = (hashCode * 397) ^ State.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(CharacterStepResult left, CharacterStepResult right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CharacterStepResult left, CharacterStepResult right)
        {
            return !left.Equals(right);
        }
    }
}
