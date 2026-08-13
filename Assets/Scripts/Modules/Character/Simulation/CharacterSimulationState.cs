using System;
using Modules.SurfaceGravity.Core;
using UnityEngine;

namespace Modules.Character.Simulation
{
    public enum JumpPhase : byte
    {
        None = 0,
        Holding = 1
    }

    public readonly struct CharacterSimulationState : IEquatable<CharacterSimulationState>
    {
        public static readonly CharacterSimulationState Initial = new CharacterSimulationState(
            GravityState.Empty,
            JumpPhase.None,
            0f,
            0f,
            Vector3.zero);

        public CharacterSimulationState(
            GravityState gravity,
            JumpPhase jumpPhase,
            float jumpElapsed,
            float viewYaw)
            : this(gravity, jumpPhase, jumpElapsed, viewYaw, Vector3.zero)
        {
        }

        public CharacterSimulationState(
            GravityState gravity,
            JumpPhase jumpPhase,
            float jumpElapsed,
            float viewYaw,
            Vector3 headingForward)
        {
            Gravity = gravity;
            JumpPhase = jumpPhase;
            JumpElapsed = jumpElapsed;
            ViewYaw = viewYaw;
            HeadingForward = headingForward;
        }

        public GravityState Gravity { get; }

        public JumpPhase JumpPhase { get; }

        public float JumpElapsed { get; }

        public float ViewYaw { get; }

        public Vector3 HeadingForward { get; }

        public CharacterSimulationState WithGravity(GravityState gravity)
        {
            return new CharacterSimulationState(
                gravity,
                JumpPhase,
                JumpElapsed,
                ViewYaw,
                HeadingForward);
        }

        public bool Equals(CharacterSimulationState other)
        {
            return Gravity.Equals(other.Gravity) &&
                   JumpPhase == other.JumpPhase &&
                   JumpElapsed.Equals(other.JumpElapsed) &&
                   ViewYaw.Equals(other.ViewYaw) &&
                   HeadingForward.Equals(other.HeadingForward);
        }

        public override bool Equals(object obj)
        {
            return obj is CharacterSimulationState other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Gravity.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)JumpPhase;
                hashCode = (hashCode * 397) ^ JumpElapsed.GetHashCode();
                hashCode = (hashCode * 397) ^ ViewYaw.GetHashCode();
                hashCode = (hashCode * 397) ^ HeadingForward.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(CharacterSimulationState left, CharacterSimulationState right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CharacterSimulationState left, CharacterSimulationState right)
        {
            return !left.Equals(right);
        }
    }
}
