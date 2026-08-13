using System;
using UnityEngine;

namespace Modules.SurfaceGravity.Core
{
    public readonly struct GravityStepInput
    {
        public GravityStepInput(
            Vector3 position,
            Vector3 bodyUp,
            GravityState previousState,
            float tickDelta)
        {
            if (!IsFinite(position))
                throw new ArgumentException("Position must be finite.", nameof(position));
            if (!IsFinite(bodyUp) || bodyUp.sqrMagnitude <= Mathf.Epsilon)
                throw new ArgumentException("Body up must be finite and non-zero.", nameof(bodyUp));
            if (!IsFinite(previousState.SmoothedUp))
                throw new ArgumentException("Previous gravity state must be finite.", nameof(previousState));
            if (!IsFinite(tickDelta) || tickDelta <= 0f)
                throw new ArgumentOutOfRangeException(nameof(tickDelta), "Tick delta must be finite and positive.");

            Position = position;
            BodyUp = bodyUp;
            PreviousState = previousState;
            TickDelta = tickDelta;
        }

        public Vector3 Position { get; }

        public Vector3 BodyUp { get; }

        public GravityState PreviousState { get; }

        public float TickDelta { get; }

        internal void Validate()
        {
            if (!IsFinite(Position) ||
                !IsFinite(BodyUp) ||
                BodyUp.sqrMagnitude <= Mathf.Epsilon ||
                !IsFinite(PreviousState.SmoothedUp) ||
                !IsFinite(TickDelta) ||
                TickDelta <= 0f)
            {
                throw new ArgumentException("Gravity step input is invalid.", nameof(GravityStepInput));
            }
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    public readonly struct GravityStepResult
    {
        public GravityStepResult(
            bool hasSurface,
            Vector3 acceleration,
            Vector3 targetUp,
            GravityState state)
        {
            if (!IsFinite(acceleration))
                throw new ArgumentException("Acceleration must be finite.", nameof(acceleration));
            if (!IsFinite(targetUp) || targetUp.sqrMagnitude <= Mathf.Epsilon)
                throw new ArgumentException("Target up must be finite and non-zero.", nameof(targetUp));
            if (!IsFinite(state.SmoothedUp))
                throw new ArgumentException("Gravity state must be finite.", nameof(state));

            HasSurface = hasSurface;
            Acceleration = acceleration;
            TargetUp = targetUp;
            State = state;
        }

        public bool HasSurface { get; }

        public Vector3 Acceleration { get; }

        public Vector3 TargetUp { get; }

        public GravityState State { get; }

        public static GravityStepResult NoSurface(GravityState previousState, Vector3 bodyUp)
        {
            return new GravityStepResult(
                false,
                Vector3.zero,
                bodyUp.normalized,
                previousState);
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
