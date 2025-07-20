using System;
using UnityEngine;

namespace Base.SurfaceGravity.Data
{
    [Serializable]
    public class SurfaceGravitySettings
    {
        [field:SerializeField]
        public float SphereRadius { get; private set; } = 0.25f;
        [field:SerializeField]
        public float MaxRotateDeg { get; private set; } = 45f;
        [field:SerializeField]
        public float RayLength { get; private set; } = 100f;
        [field:SerializeField]
        public float NormalLerpSpeed { get; private set; } = 6f;
        
        [field:SerializeField]
        public float GravityAcceleration { get; private set; } = 9.81f;
    }
}