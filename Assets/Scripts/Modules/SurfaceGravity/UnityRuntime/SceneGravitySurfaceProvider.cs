using System;
using System.Collections.Generic;
using Modules.SurfaceGravity.Core;
using UnityEngine;

namespace Modules.SurfaceGravity.UnityRuntime
{
    public sealed class SceneGravitySurfaceProvider : IGravitySurfaceProvider
    {
        private readonly Dictionary<Collider, AuthoredSurface> _surfacesByCollider;
        private readonly AuthoredSurface[] _surfaces;
        private readonly Collider[] _overlapBuffer;
        private readonly float _searchRadius;

        public SceneGravitySurfaceProvider(
            IEnumerable<GravitySurfaceView> views,
            float searchRadius,
            int maxOverlaps)
        {
            if (views == null)
                throw new ArgumentNullException(nameof(views));
            if (!IsFinite(searchRadius) || searchRadius <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(searchRadius),
                    "Gravity-surface search radius must be finite and positive.");
            }
            if (maxOverlaps <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxOverlaps), "Overlap capacity must be positive.");

            _searchRadius = searchRadius;
            _overlapBuffer = new Collider[maxOverlaps];
            _surfacesByCollider = new Dictionary<Collider, AuthoredSurface>();

            var surfaces = new List<AuthoredSurface>();
            var surfaceIds = new HashSet<SurfaceId>();
            foreach (var view in views)
            {
                if (view == null)
                    throw new InvalidOperationException("A gravity-surface view reference is missing.");

                var surfaceId = view.SurfaceId;
                if (!surfaceId.IsValid)
                {
                    throw new InvalidOperationException(
                        $"Gravity surface '{view.name}' has a blank authored surface ID.");
                }
                if (!surfaceIds.Add(surfaceId))
                {
                    throw new InvalidOperationException(
                        $"Duplicate authored gravity surface ID '{surfaceId.Value}'.");
                }

                var gravityCollider = view.GravityCollider;
                if (gravityCollider == null)
                {
                    throw new InvalidOperationException(
                        $"Gravity surface '{surfaceId.Value}' has no collider assigned.");
                }

                var surface = new AuthoredSurface(
                    surfaceId,
                    gravityCollider,
                    gravityCollider.transform.localToWorldMatrix);
                if (!_surfacesByCollider.TryAdd(gravityCollider, surface))
                {
                    throw new InvalidOperationException(
                        $"Collider '{gravityCollider.name}' is assigned to more than one gravity surface.");
                }

                surfaces.Add(surface);
            }

            _surfaces = surfaces.ToArray();
        }

        public void GetSamples(Vector3 worldPosition, List<GravitySurfaceSample> samples)
        {
            if (samples == null)
                throw new ArgumentNullException(nameof(samples));
            if (!IsFinite(worldPosition))
                throw new ArgumentException("World position must be finite.", nameof(worldPosition));

            ValidateStaticTransforms();
            samples.Clear();

            var overlapCount = Physics.OverlapSphereNonAlloc(
                worldPosition,
                _searchRadius,
                _overlapBuffer,
                Physics.AllLayers,
                QueryTriggerInteraction.Collide);
            if (overlapCount == _overlapBuffer.Length)
            {
                throw new InvalidOperationException(
                    $"Gravity-surface overlap capacity {_overlapBuffer.Length} was exhausted. " +
                    "Increase max overlaps on SurfaceGravityInstaller.");
            }

            for (var index = 0; index < overlapCount; index++)
            {
                var collider = _overlapBuffer[index];
                _overlapBuffer[index] = null;
                if (collider == null || !_surfacesByCollider.TryGetValue(collider, out var surface))
                    continue;

                var closestPoint = collider.ClosestPoint(worldPosition);
                var fromSurface = worldPosition - closestPoint;
                var sqrDistance = fromSurface.sqrMagnitude;
                var outwardNormal = sqrDistance > Mathf.Epsilon
                    ? fromSurface / Mathf.Sqrt(sqrDistance)
                    : GetInteriorFallbackNormal(collider, worldPosition);

                samples.Add(new GravitySurfaceSample(
                    surface.SurfaceId,
                    closestPoint,
                    outwardNormal,
                    sqrDistance));
            }

            samples.Sort(GravitySurfaceSampleComparer.Instance);
        }

        private static Vector3 GetInteriorFallbackNormal(Collider collider, Vector3 worldPosition)
        {
            var fromCenter = worldPosition - collider.bounds.center;
            return fromCenter.sqrMagnitude > Mathf.Epsilon
                ? fromCenter.normalized
                : collider.transform.up;
        }

        private void ValidateStaticTransforms()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            for (var index = 0; index < _surfaces.Length; index++)
            {
                var surface = _surfaces[index];
                if (surface.Collider == null)
                {
                    throw new InvalidOperationException(
                        $"Gravity surface '{surface.SurfaceId.Value}' was destroyed after initialization.");
                }
                if (!surface.Collider.transform.localToWorldMatrix.Equals(surface.InitialLocalToWorld))
                {
                    throw new InvalidOperationException(
                        $"Gravity surface '{surface.SurfaceId.Value}' moved after initialization. " +
                        "Moving gravity sources are not supported.");
                }
            }
#endif
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private sealed class AuthoredSurface
        {
            public AuthoredSurface(
                SurfaceId surfaceId,
                Collider collider,
                Matrix4x4 initialLocalToWorld)
            {
                SurfaceId = surfaceId;
                Collider = collider;
                InitialLocalToWorld = initialLocalToWorld;
            }

            public SurfaceId SurfaceId { get; }

            public Collider Collider { get; }

            public Matrix4x4 InitialLocalToWorld { get; }
        }

        private sealed class GravitySurfaceSampleComparer : IComparer<GravitySurfaceSample>
        {
            public static readonly GravitySurfaceSampleComparer Instance = new GravitySurfaceSampleComparer();

            public int Compare(GravitySurfaceSample left, GravitySurfaceSample right)
            {
                var distanceComparison = left.SqrDistance.CompareTo(right.SqrDistance);
                return distanceComparison != 0
                    ? distanceComparison
                    : StringComparer.Ordinal.Compare(left.SurfaceId.Value, right.SurfaceId.Value);
            }
        }
    }
}
