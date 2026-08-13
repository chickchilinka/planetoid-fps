using System;
using System.Collections.Generic;
using UnityEngine;

namespace Modules.SurfaceGravity.Core
{
    public sealed class SurfaceGravitySolver : ISurfaceGravitySolver
    {
        private readonly List<GravitySurfaceSample> _samples = new List<GravitySurfaceSample>();
        private readonly IGravitySurfaceProvider _surfaceProvider;
        private readonly SurfaceGravitySettings _settings;

        public SurfaceGravitySolver(
            IGravitySurfaceProvider surfaceProvider,
            SurfaceGravitySettings settings)
        {
            _surfaceProvider = surfaceProvider ?? throw new ArgumentNullException(nameof(surfaceProvider));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public GravityStepResult Solve(in GravityStepInput input)
        {
            input.Validate();
            _samples.Clear();
            _surfaceProvider.GetSamples(input.Position, _samples);

            if (_samples.Count == 0)
                return GravityStepResult.NoSurface(input.PreviousState, input.BodyUp);

            ValidateSamples();
            _samples.Sort(GravitySurfaceSampleComparer.Instance);

            var selected = _samples[0];
            if (input.PreviousState.ActiveSurface.IsValid &&
                TryFind(input.PreviousState.ActiveSurface, out var current) &&
                current.SqrDistance <= selected.SqrDistance + _settings.SwitchHysteresisSqr)
            {
                selected = current;
            }

            var targetUp = selected.OutwardNormal.normalized;
            var hasReplayableUp = input.PreviousState.ActiveSurface.IsValid &&
                                  input.PreviousState.SmoothedUp.sqrMagnitude > 0.5f;
            var from = hasReplayableUp
                ? input.PreviousState.SmoothedUp.normalized
                : input.BodyUp.normalized;
            var blend = 1f - Mathf.Exp(-_settings.NormalSharpness * input.TickDelta);
            var smoothedUp = Vector3.Slerp(from, targetUp, blend).normalized;
            var state = new GravityState(selected.SurfaceId, smoothedUp);

            return new GravityStepResult(
                true,
                -targetUp * _settings.Acceleration,
                smoothedUp,
                state);
        }

        private void ValidateSamples()
        {
            for (var index = 0; index < _samples.Count; index++)
                _samples[index].Validate();
        }

        private bool TryFind(SurfaceId surfaceId, out GravitySurfaceSample sample)
        {
            for (var index = 0; index < _samples.Count; index++)
            {
                if (_samples[index].SurfaceId != surfaceId)
                    continue;

                sample = _samples[index];
                return true;
            }

            sample = default;
            return false;
        }

        private sealed class GravitySurfaceSampleComparer : IComparer<GravitySurfaceSample>
        {
            public static readonly GravitySurfaceSampleComparer Instance = new GravitySurfaceSampleComparer();

            public int Compare(GravitySurfaceSample left, GravitySurfaceSample right)
            {
                var distanceComparison = left.SqrDistance.CompareTo(right.SqrDistance);
                if (distanceComparison != 0)
                    return distanceComparison;

                return StringComparer.Ordinal.Compare(left.SurfaceId.Value, right.SurfaceId.Value);
            }
        }
    }
}
