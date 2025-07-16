using Base.RigidbodyMovement.Providers;
using Base.SurfaceGravity.Services;
using UnityEngine;
using Zenject;

namespace Features.Character.Movement.Provider
{
    public class MovementDirectionProvider: IMovementDirectionProvider
    {
        public Vector3 Up => _surfaceGravityService.GetBodyUp(_characterRigidbody);
        public Vector3 Forward => _cameraRoot.forward;
        
        private readonly SurfaceGravityService _surfaceGravityService;
        private Rigidbody _characterRigidbody;
        private Transform _cameraRoot;

        public MovementDirectionProvider(SurfaceGravityService surfaceGravityService)
        {
            _surfaceGravityService = surfaceGravityService;
        }
        
        
        //TODO: Get local player gameobject and local camera root by code
        public void Initialize(Rigidbody characterRigidbody, Transform cameraRoot)
        {
            _characterRigidbody = characterRigidbody;
            _cameraRoot = cameraRoot;
        }
    }
}