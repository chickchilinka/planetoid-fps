using System;
using Modules.SurfaceGravity.Core;
using UnityEngine;

namespace Modules.Character.Simulation
{
    public readonly struct CharacterGravityQuery : IEquatable<CharacterGravityQuery>
    {
        public CharacterGravityQuery(
            Vector3 position,
            Vector3 bodyUp,
            GravityState previousState,
            float tickDelta)
        {
            Position = position;
            BodyUp = bodyUp;
            PreviousState = previousState;
            TickDelta = tickDelta;
        }

        public Vector3 Position { get; }

        public Vector3 BodyUp { get; }

        public GravityState PreviousState { get; }

        public float TickDelta { get; }

        public bool Equals(CharacterGravityQuery other)
        {
            return Position.Equals(other.Position) &&
                   BodyUp.Equals(other.BodyUp) &&
                   PreviousState.Equals(other.PreviousState) &&
                   TickDelta.Equals(other.TickDelta);
        }

        public override bool Equals(object obj)
        {
            return obj is CharacterGravityQuery other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Position.GetHashCode();
                hashCode = (hashCode * 397) ^ BodyUp.GetHashCode();
                hashCode = (hashCode * 397) ^ PreviousState.GetHashCode();
                hashCode = (hashCode * 397) ^ TickDelta.GetHashCode();
                return hashCode;
            }
        }
    }

    public readonly struct CharacterGravityResult : IEquatable<CharacterGravityResult>
    {
        public CharacterGravityResult(
            Vector3 acceleration,
            Vector3 targetUp,
            GravityState state)
        {
            Acceleration = acceleration;
            TargetUp = targetUp;
            State = state;
        }

        public Vector3 Acceleration { get; }

        public Vector3 TargetUp { get; }

        public GravityState State { get; }

        public bool Equals(CharacterGravityResult other)
        {
            return Acceleration.Equals(other.Acceleration) &&
                   TargetUp.Equals(other.TargetUp) &&
                   State.Equals(other.State);
        }

        public override bool Equals(object obj)
        {
            return obj is CharacterGravityResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Acceleration.GetHashCode();
                hashCode = (hashCode * 397) ^ TargetUp.GetHashCode();
                hashCode = (hashCode * 397) ^ State.GetHashCode();
                return hashCode;
            }
        }
    }

    public interface ICharacterGravityProvider
    {
        CharacterGravityResult Solve(in CharacterGravityQuery query);
    }
}
