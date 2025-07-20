using Base.RigidbodyMovement;
using Base.RigidbodyMovement.Data;
using Base.SurfaceGravity.View;
using Features.Character.Movement.Provider;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject;

namespace Features.Character.Movement.Bootstrap
{
    public class CharacterControllerInstaller: MonoInstaller
    {
        [SerializeField] 
        private MovementSettings _movementSettings;
        [SerializeField]
        private GravityBodyView _characterBodyView;
        [SerializeField]
        private Transform _cameraRoot;
        public override void InstallBindings()
        {
            Container.BindInstance(_movementSettings).AsSingle();
            Container.BindInterfacesAndSelfTo<RigidbodyMovementController>().AsSingle().OnInstantiated((_, obj) =>
            {
                var controller = obj as RigidbodyMovementController;
                controller!.SetRigidBody(_characterBodyView.Rigidbody);
            });
            Container.BindInterfacesAndSelfTo<MovementInputProvider>().AsSingle();
            Container.BindInterfacesAndSelfTo<MovementDirectionProvider>().AsSingle().OnInstantiated((_, obj) =>
            {
                var provider = obj as MovementDirectionProvider;
                provider!.Initialize(_characterBodyView.Id, _cameraRoot);
            });
        }
    }
}