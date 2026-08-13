using System.Collections.Generic;
using Modules.SurfaceGravity.Core;
using UnityEngine;

namespace Modules.SurfaceGravity.Tests
{
    internal sealed class FakeGravitySurfaceProvider : IGravitySurfaceProvider
    {
        private readonly GravitySurfaceSample[] _samples;

        public FakeGravitySurfaceProvider(params GravitySurfaceSample[] samples)
        {
            _samples = samples;
        }

        public void GetSamples(Vector3 worldPosition, List<GravitySurfaceSample> samples)
        {
            samples.AddRange(_samples);
        }
    }
}
