using Base.RigidbodyMovement.Providers;
using Base.SurfaceGravity.Services;
using Base.SurfaceGravity.View;
using UnityEngine;
using Zenject;

namespace Features.Character.Movement.Provider
{
    public class MovementDirectionProvider: IMovementDirectionProvider
    {
        public Vector3 Up => _surfaceGravityService.GetBodyUp(_bodyId);
        public Vector3 Forward => _cameraRoot.forward;
        
        private readonly SurfaceGravityService _surfaceGravityService;
        private string _bodyId;
        private Transform _cameraRoot;

        public MovementDirectionProvider(SurfaceGravityService surfaceGravityService)
        {
            _surfaceGravityService = surfaceGravityService;
        }
        
        public void Initialize(string bodyId, Transform cameraRoot)
        {
            _bodyId = bodyId;
            _cameraRoot = cameraRoot;
        }
    }
}