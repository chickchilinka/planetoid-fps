using System;
using Modules.Character.Simulation;
using UnityEngine;
using Zenject;

namespace Modules.Character.UnityRuntime
{
    public sealed class OfflineCharacterInstaller : MonoInstaller
    {
        [Header("Runtime adapters")]
        [SerializeField] private OfflineCharacterController _controller;
        [SerializeField] private TransformCharacterViewYawProvider _viewYawProvider;
        [SerializeField] private PhysicsCharacterGroundProbe _groundProbe;

        [Header("Movement")]
        [SerializeField, Min(0.001f)] private float _moveSpeed = 5f;
        [SerializeField, Min(0.001f)] private float _initialJumpSpeed = 8f;
        [SerializeField, Min(0f)] private float _holdJumpAcceleration = 4f;
        [SerializeField, Min(0f)] private float _airAcceleration = 3f;
        [SerializeField, Min(0.001f)] private float _maxJumpHoldTime = 1f;
        [SerializeField, Min(0.001f)] private float _maxRotationDegreesPerTick = 45f;

        public override void InstallBindings()
        {
            ValidateReferences();

            var settings = new CharacterMovementSettings(
                _moveSpeed,
                _initialJumpSpeed,
                _holdJumpAcceleration,
                _airAcceleration,
                _maxJumpHoldTime,
                _maxRotationDegreesPerTick);

            Container.BindInstance(settings).AsSingle();
            Container.Bind<ICharacterGravityProvider>()
                .To<CharacterSurfaceGravityAdapter>()
                .AsSingle();
            Container.Bind<ICharacterSimulationService>()
                .To<CharacterSimulationService>()
                .AsSingle();
            Container.Bind<ICharacterViewYawProvider>()
                .FromInstance(_viewYawProvider)
                .AsSingle();
            Container.BindInterfacesAndSelfTo<UnityCharacterInputSource>()
                .AsSingle();
            Container.Bind<ICharacterGroundProbe>()
                .FromInstance(_groundProbe)
                .AsSingle();
            Container.Bind<OfflineCharacterController>()
                .FromInstance(_controller)
                .AsSingle();
        }

        private void ValidateReferences()
        {
            if (_controller == null)
                throw new InvalidOperationException("An offline character controller is required.");
            if (_viewYawProvider == null)
                throw new InvalidOperationException("A character view-yaw provider is required.");
            if (_groundProbe == null)
                throw new InvalidOperationException("A character ground probe is required.");
        }
    }
}
