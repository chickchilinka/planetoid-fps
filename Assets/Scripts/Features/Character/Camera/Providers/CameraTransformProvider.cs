using Base.ThirdPersonCamera.Providers;
using UnityEngine;

namespace Features.Character.Camera.Providers
{
    public class CameraTransformProvider : MonoBehaviour, ICameraTransformProvider
    {
        [SerializeField] private Transform _yawPivot;
        [SerializeField] private Transform _yawUp;

        public Vector3 YawPivot => _yawPivot.position;

        public Vector3 YawUp => _yawUp.up;
    }
}