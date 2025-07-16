using Base.ThirdPersonCamera.Data;
using Base.ThirdPersonCamera.Providers;
using UnityEngine;
using Zenject;

namespace Base.ThirdPersonCamera
{
    public class ThirdPersonCameraController : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private Transform _yawTransform;
        [SerializeField] private Transform _pitchTransform;

        private ICameraInputProvider _cameraInputProvider;
        private ICameraTransformProvider _cameraTransformProvider;
        private ThirdPersonCameraSettings _settings;

        private float _yaw;
        private float _pitch;

        private Quaternion _referenceRotation;

        [Inject]
        public void Construct(ICameraInputProvider inputProvider, ICameraTransformProvider transformProvider,
            ThirdPersonCameraSettings thirdPersonCameraSettings)
        {
            _cameraInputProvider = inputProvider;
            _cameraTransformProvider = transformProvider;
            _settings = thirdPersonCameraSettings;
        }

        private void Start()
        {
            var up = _yawTransform.up;
            var forward = Vector3.ProjectOnPlane(transform.forward, up).normalized;
            _referenceRotation = Quaternion.LookRotation(forward, up);
        }

        private void LateUpdate()
        {
            UpdateRotation();
            UpdatePosition();
        }

        private void UpdateRotation()
        {
            var lookDelta = _cameraInputProvider.LookDelta * _settings.DefaultSensitivity;
            _yaw += lookDelta.x;
            _pitch -= lookDelta.y * (_settings.InvertY ? -1 : 1);
            _pitch = Mathf.Clamp(_pitch, _settings.MinPitch, _settings.MaxPitch);
            
            var up = _cameraTransformProvider.YawUp;

            _referenceRotation = Quaternion.Slerp(_referenceRotation,
                Quaternion.LookRotation(Vector3.ProjectOnPlane(_referenceRotation * Vector3.forward, up).normalized,
                    up), Time.deltaTime * _settings.YawFollowSpeed);
            
            Quaternion yawRotation = _referenceRotation * Quaternion.AngleAxis(_yaw, Vector3.up);
            _yawTransform.rotation = yawRotation;
            _pitchTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void UpdatePosition()
        {
            _yawTransform.position = Vector3.MoveTowards(_yawTransform.position, _cameraTransformProvider.YawPivot,
                Time.deltaTime * _settings.FollowSpeed);
        }
    }
}