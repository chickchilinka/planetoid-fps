using System;

namespace Modules.SurfaceGravity.Core
{
    public sealed class SurfaceGravitySettings
    {
        public static readonly SurfaceGravitySettings Default = new SurfaceGravitySettings(
            acceleration: 9.81f,
            normalSharpness: 6f,
            switchHysteresisSqr: 0.25f);

        public SurfaceGravitySettings(
            float acceleration,
            float normalSharpness,
            float switchHysteresisSqr)
        {
            if (!IsFinite(acceleration) || acceleration <= 0f)
                throw new ArgumentOutOfRangeException(nameof(acceleration), "Acceleration must be finite and positive.");
            if (!IsFinite(normalSharpness) || normalSharpness <= 0f)
                throw new ArgumentOutOfRangeException(nameof(normalSharpness), "Normal sharpness must be finite and positive.");
            if (!IsFinite(switchHysteresisSqr) || switchHysteresisSqr < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(switchHysteresisSqr),
                    "Squared switch hysteresis must be finite and non-negative.");
            }

            Acceleration = acceleration;
            NormalSharpness = normalSharpness;
            SwitchHysteresisSqr = switchHysteresisSqr;
        }

        public float Acceleration { get; }

        public float NormalSharpness { get; }

        public float SwitchHysteresisSqr { get; }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
