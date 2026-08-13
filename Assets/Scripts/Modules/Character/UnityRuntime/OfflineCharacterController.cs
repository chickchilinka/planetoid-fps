using System;
using Modules.Character.Simulation;
using UnityEngine;
using Zenject;

namespace Modules.Character.UnityRuntime
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class OfflineCharacterController : MonoBehaviour
    {
        private Rigidbody _rigidbody;
        private ICharacterSimulationService _simulation;
        private ICharacterInputSource _input;
        private ICharacterGroundProbe _groundProbe;
        private CharacterMovementSettings _settings;
        private CharacterSimulationState _state = CharacterSimulationState.Initial;

        [Inject]
        public void Construct(
            ICharacterSimulationService simulation,
            ICharacterInputSource input,
            ICharacterGroundProbe groundProbe,
            CharacterMovementSettings settings)
        {
            _simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _groundProbe = groundProbe ?? throw new ArgumentNullException(nameof(groundProbe));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.useGravity = false;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _rigidbody.constraints |= RigidbodyConstraints.FreezeRotation;
        }

        private void FixedUpdate()
        {
            EnsureConstructed();

            var up = _state.Gravity.ActiveSurface.IsValid &&
                     _state.Gravity.SmoothedUp.sqrMagnitude > 0.5f
                ? _state.Gravity.SmoothedUp.normalized
                : _rigidbody.rotation * Vector3.up;
            var grounded = _groundProbe.IsGrounded(
                _rigidbody.position,
                up,
                out var groundNormal);
            var body = new CharacterBodySnapshot(
                _rigidbody.position,
                _rigidbody.rotation,
                _rigidbody.linearVelocity,
                grounded,
                groundNormal);
            var result = _simulation.Simulate(
                _input.ConsumeForTick(),
                body,
                _state,
                Time.fixedDeltaTime);

            Apply(result);
            _state = result.State;
        }

        private void Apply(in CharacterStepResult result)
        {
            _rigidbody.linearVelocity = result.LinearVelocity;
            _rigidbody.AddForce(result.Acceleration, ForceMode.Acceleration);
            _rigidbody.MoveRotation(Quaternion.RotateTowards(
                _rigidbody.rotation,
                result.TargetRotation,
                _settings.MaxRotationDegreesPerTick));
        }

        private void EnsureConstructed()
        {
            if (_simulation == null || _input == null || _groundProbe == null || _settings == null)
            {
                throw new InvalidOperationException(
                    "OfflineCharacterController must be composed by OfflineCharacterInstaller before simulation.");
            }
        }
    }
}
