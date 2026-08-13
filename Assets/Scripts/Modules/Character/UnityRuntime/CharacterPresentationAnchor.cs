using System;
using UnityEngine;

namespace Modules.Character.UnityRuntime
{
    [DefaultExecutionOrder(-100)]
    public sealed class CharacterPresentationAnchor : MonoBehaviour
    {
        [SerializeField] private Transform _physicsRoot;
        [SerializeField, Min(0.001f)] private float _positionSharpness = 20f;
        [SerializeField, Min(0.001f)] private float _rotationSharpness = 20f;

        private Vector3 _positionOffset;
        private Quaternion _rotationOffset;
        private Vector3 _presentationPosition;
        private Quaternion _presentationRotation;

        private void Awake()
        {
            Initialize(_physicsRoot);
        }

        private void LateUpdate()
        {
            UpdatePresentation(Time.deltaTime);
        }

        internal void UpdatePresentation(float deltaTime)
        {
            var targetPosition = _physicsRoot.TransformPoint(_positionOffset);
            var targetRotation = _physicsRoot.rotation * _rotationOffset;
            var positionAlpha = 1f - Mathf.Exp(-_positionSharpness * deltaTime);
            var rotationAlpha = 1f - Mathf.Exp(-_rotationSharpness * deltaTime);
            _presentationPosition = Vector3.Lerp(
                _presentationPosition,
                targetPosition,
                positionAlpha);
            _presentationRotation = Quaternion.Slerp(
                _presentationRotation,
                targetRotation,
                rotationAlpha);
            transform.SetPositionAndRotation(_presentationPosition, _presentationRotation);
        }

        internal void ConfigureForTests(
            Transform physicsRoot,
            float positionSharpness = 20f,
            float rotationSharpness = 20f)
        {
            _physicsRoot = physicsRoot;
            _positionSharpness = positionSharpness;
            _rotationSharpness = rotationSharpness;
        }

        internal void Initialize(Transform physicsRoot)
        {
            if (physicsRoot == null)
                throw new InvalidOperationException("A physics root is required for presentation smoothing.");
            if (transform == physicsRoot)
            {
                throw new InvalidOperationException(
                    "The presentation anchor must not be the physics root itself.");
            }

            _physicsRoot = physicsRoot;
            _positionOffset = physicsRoot.InverseTransformPoint(transform.position);
            _rotationOffset = Quaternion.Inverse(physicsRoot.rotation) * transform.rotation;
            _presentationPosition = transform.position;
            _presentationRotation = transform.rotation;
        }
    }
}
