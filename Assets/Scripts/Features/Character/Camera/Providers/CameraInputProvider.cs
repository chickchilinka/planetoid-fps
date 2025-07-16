using Base.Input.Services;
using Base.ThirdPersonCamera.Providers;
using UnityEngine;

namespace Features.Character.Camera.Providers
{
    public class CameraInputProvider: ICameraInputProvider
    {
        public Vector2 LookDelta => _inputService.GetMouseDelta();
        private readonly InputService _inputService;
        
        public CameraInputProvider(InputService inputService)
        {
            _inputService = inputService;
        }
    }
}