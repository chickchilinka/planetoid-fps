using System;
using UnityEngine;

namespace Modules.Character.UnityRuntime
{
    public sealed class TransformCharacterViewYawProvider : MonoBehaviour, ICharacterViewYawProvider
    {
        private const float DirectionEpsilon = 0.000001f;

        [SerializeField] private Transform _yawTransform;

        private bool _hasPreviousHeading;
        private Vector3 _previousHeading;
        private Vector3 _previousUp;
        private float _yaw;

        public float Yaw
        {
            get
            {
                SampleTransform();
                return _yaw;
            }
        }

        private void Awake()
        {
            if (_yawTransform == null)
                throw new InvalidOperationException("A camera yaw transform is required.");
        }

        private void SampleTransform()
        {
            if (_yawTransform == null)
                throw new InvalidOperationException("A camera yaw transform is required.");

            var up = _yawTransform.up;
            var heading = Vector3.ProjectOnPlane(_yawTransform.forward, up);
            if (heading.sqrMagnitude <= DirectionEpsilon)
                return;

            heading.Normalize();
            if (_hasPreviousHeading)
            {
                var transportedHeading =
                    Quaternion.FromToRotation(_previousUp, up) * _previousHeading;
                transportedHeading = Vector3.ProjectOnPlane(transportedHeading, up);
                if (transportedHeading.sqrMagnitude > DirectionEpsilon)
                {
                    _yaw += Vector3.SignedAngle(
                        transportedHeading.normalized,
                        heading,
                        up);
                }
            }

            _previousHeading = heading;
            _previousUp = up.normalized;
            _hasPreviousHeading = true;
        }
    }
}
