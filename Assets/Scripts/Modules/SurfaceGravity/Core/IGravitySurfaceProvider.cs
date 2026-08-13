using System.Collections.Generic;
using UnityEngine;

namespace Modules.SurfaceGravity.Core
{
    public interface IGravitySurfaceProvider
    {
        void GetSamples(Vector3 worldPosition, List<GravitySurfaceSample> samples);
    }
}
