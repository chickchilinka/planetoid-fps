using System;

namespace Base.RigidbodyMovement.Data
{
    [Serializable]
    public class MovementSettings
    {
        public float MoveSpeed = 5f;
        public float InitialJumpForce = 8f;
        public float HoldJumpForce = 8f;
        public float AirControl = 0.3f;
        public float GroundCheckDistance = 1.1f;
        public float MaxJumpHoldTime = 0.3f;
    }
}