using System;
using UnityEngine;

namespace Modules.SurfaceGravity.Core
{
    public readonly struct GravitySurfaceSample
    {
        public GravitySurfaceSample(
            SurfaceId surfaceId,
            Vector3 closestPoint,
            Vector3 outwardNormal,
            float sqrDistance)
        {
            if (!surfaceId.IsValid)
                throw new ArgumentException("A gravity surface sample requires a valid authored ID.", nameof(surfaceId));
            if (!IsFinite(closestPoint))
                throw new ArgumentException("The closest point must be finite.", nameof(closestPoint));
            if (!IsFinite(outwardNormal) || outwardNormal.sqrMagnitude <= Mathf.Epsilon)
                throw new ArgumentException("The outward normal must be finite and non-zero.", nameof(outwardNormal));
            if (!IsFinite(sqrDistance) || sqrDistance < 0f)
                throw new ArgumentOutOfRangeException(nameof(sqrDistance), "Squared distance must be finite and non-negative.");

            SurfaceId = surfaceId;
            ClosestPoint = closestPoint;
            OutwardNormal = outwardNormal;
            SqrDistance = sqrDistance;
        }

        public SurfaceId SurfaceId { get; }

        public Vector3 ClosestPoint { get; }

        public Vector3 OutwardNormal { get; }

        public float SqrDistance { get; }

        internal void Validate()
        {
            if (!SurfaceId.IsValid ||
                !IsFinite(ClosestPoint) ||
                !IsFinite(OutwardNormal) ||
                OutwardNormal.sqrMagnitude <= Mathf.Epsilon ||
                !IsFinite(SqrDistance) ||
                SqrDistance < 0f)
            {
                throw new InvalidOperationException("The gravity surface provider returned an invalid sample.");
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
}
