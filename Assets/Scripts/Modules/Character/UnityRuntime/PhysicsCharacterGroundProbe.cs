using System;
using UnityEngine;

namespace Modules.Character.UnityRuntime
{
    public sealed class PhysicsCharacterGroundProbe : MonoBehaviour, ICharacterGroundProbe
    {
        [SerializeField, Min(0.001f)] private float _groundCheckDistance = 1.1f;
        [SerializeField] private LayerMask _groundMask = ~0;

        public bool IsGrounded(Vector3 position, Vector3 up, out Vector3 normal)
        {
            if (!IsFinite(position))
                throw new ArgumentException("Probe position must be finite.", nameof(position));
            if (!IsFinite(up) || up.sqrMagnitude <= Mathf.Epsilon)
                throw new ArgumentException("Probe up must be finite and non-zero.", nameof(up));

            up.Normalize();
            if (Physics.Raycast(
                    position,
                    -up,
                    out var hit,
                    _groundCheckDistance,
                    _groundMask,
                    QueryTriggerInteraction.Ignore))
            {
                normal = hit.normal;
                return true;
            }

            normal = up;
            return false;
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
