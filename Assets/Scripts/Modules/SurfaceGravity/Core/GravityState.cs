using System;
using UnityEngine;

namespace Modules.SurfaceGravity.Core
{
    public readonly struct GravityState : IEquatable<GravityState>
    {
        public static readonly GravityState Empty = new GravityState(SurfaceId.None, Vector3.up);

        public GravityState(SurfaceId activeSurface, Vector3 smoothedUp)
        {
            if (!IsFinite(smoothedUp))
                throw new ArgumentException("The smoothed up vector must be finite.", nameof(smoothedUp));

            ActiveSurface = activeSurface;
            SmoothedUp = smoothedUp;
        }

        public SurfaceId ActiveSurface { get; }

        public Vector3 SmoothedUp { get; }

        public bool Equals(GravityState other)
        {
            return ActiveSurface.Equals(other.ActiveSurface) && SmoothedUp.Equals(other.SmoothedUp);
        }

        public override bool Equals(object obj)
        {
            return obj is GravityState other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (ActiveSurface.GetHashCode() * 397) ^ SmoothedUp.GetHashCode();
            }
        }

        public static bool operator ==(GravityState left, GravityState right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GravityState left, GravityState right)
        {
            return !left.Equals(right);
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
