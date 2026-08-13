using System;

namespace Modules.Character.Simulation
{
    public sealed class CharacterMovementSettings
    {
        public static readonly CharacterMovementSettings Default = new CharacterMovementSettings(
            moveSpeed: 5f,
            initialJumpSpeed: 8f,
            holdJumpAcceleration: 4f,
            airAcceleration: 3f,
            maxJumpHoldTime: 1f,
            maxRotationDegreesPerTick: 45f);

        public CharacterMovementSettings(
            float moveSpeed,
            float initialJumpSpeed,
            float holdJumpAcceleration,
            float airAcceleration,
            float maxJumpHoldTime,
            float maxRotationDegreesPerTick)
        {
            if (!IsFinite(moveSpeed) || moveSpeed <= 0f)
                throw new ArgumentOutOfRangeException(nameof(moveSpeed), "Move speed must be finite and positive.");
            if (!IsFinite(initialJumpSpeed) || initialJumpSpeed <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(initialJumpSpeed),
                    "Initial jump speed must be finite and positive.");
            }

            if (!IsFinite(holdJumpAcceleration) || holdJumpAcceleration < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(holdJumpAcceleration),
                    "Hold jump acceleration must be finite and non-negative.");
            }

            if (!IsFinite(airAcceleration) || airAcceleration < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(airAcceleration),
                    "Air acceleration must be finite and non-negative.");
            }

            if (!IsFinite(maxJumpHoldTime) || maxJumpHoldTime <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxJumpHoldTime),
                    "Maximum jump hold time must be finite and positive.");
            }

            if (!IsFinite(maxRotationDegreesPerTick) || maxRotationDegreesPerTick <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxRotationDegreesPerTick),
                    "Maximum rotation per tick must be finite and positive.");
            }

            MoveSpeed = moveSpeed;
            InitialJumpSpeed = initialJumpSpeed;
            HoldJumpAcceleration = holdJumpAcceleration;
            AirAcceleration = airAcceleration;
            MaxJumpHoldTime = maxJumpHoldTime;
            MaxRotationDegreesPerTick = maxRotationDegreesPerTick;
        }

        public float MoveSpeed { get; }

        public float InitialJumpSpeed { get; }

        public float HoldJumpAcceleration { get; }

        public float AirAcceleration { get; }

        public float MaxJumpHoldTime { get; }

        public float MaxRotationDegreesPerTick { get; }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
