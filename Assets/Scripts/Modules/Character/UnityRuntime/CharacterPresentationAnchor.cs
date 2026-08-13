using System;
using UnityEngine;

namespace Modules.Character.UnityRuntime
{
    public sealed class CharacterPresentationAnchor : MonoBehaviour
    {
        [SerializeField] private Transform _physicsRoot;
        [SerializeField, Min(0.001f)] private float _positionSharpness = 20f;
        [SerializeField, Min(0.001f)] private float _rotationSharpness = 20f;

        private Vector3 _presentationPosition;
        private Quaternion _presentationRotation;

        private void Awake()
        {
            if (_physicsRoot == null)
                throw new InvalidOperationException("A physics root is required for presentation smoothing.");

            _presentationPosition = transform.position;
            _presentationRotation = transform.rotation;
        }

        private void LateUpdate()
        {
            var positionAlpha = 1f - Mathf.Exp(-_positionSharpness * Time.deltaTime);
            var rotationAlpha = 1f - Mathf.Exp(-_rotationSharpness * Time.deltaTime);
            _presentationPosition = Vector3.Lerp(
                _presentationPosition,
                _physicsRoot.position,
                positionAlpha);
            _presentationRotation = Quaternion.Slerp(
                _presentationRotation,
                _physicsRoot.rotation,
                rotationAlpha);
            transform.SetPositionAndRotation(_presentationPosition, _presentationRotation);
        }
    }
}
