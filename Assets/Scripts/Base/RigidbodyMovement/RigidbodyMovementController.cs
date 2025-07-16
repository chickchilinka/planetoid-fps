using System;
using Base.RigidbodyMovement.Data;
using Base.RigidbodyMovement.Providers;
using Zenject;
using UnityEngine;
using UniRx;


namespace Base.RigidbodyMovement
{
    public class RigidbodyMovementController : IInitializable, IFixedTickable, IDisposable
    {
        private readonly IMovementInputProvider _inputProvider;
        private readonly IMovementDirectionProvider _movementDirectionProvider;
        private readonly MovementSettings _settings;

        private readonly CompositeDisposable _subscriptions = new();

        private Rigidbody _rigidbody;
        private bool _isGrounded;
        private bool _jumpHeld;
        private bool _isJumping;
        private bool _jumpPressedThisFrame;
        private float _jumpTime;

        public RigidbodyMovementController(
            IMovementInputProvider inputProvider,
            MovementSettings settings,
            IMovementDirectionProvider movementDirectionProvider)
        {
            _inputProvider = inputProvider;
            _settings = settings;
            _movementDirectionProvider = movementDirectionProvider;
        }

        public void Initialize()
        {
            _inputProvider.JumpInput
                .Pairwise()
                .Subscribe(pair =>
                {
                    if (pair is { Previous: false, Current: true })
                    {
                        _jumpPressedThisFrame = true;
                    }
                    else if (pair is { Previous: true, Current: false })
                    {
                        _jumpHeld = false;
                    }
                })
                .AddTo(_subscriptions);
        }
        
        public void FixedTick()
        {
            if (!_rigidbody)
                return;

            UpdateGroundedStatus();
            HandleMovement();
            HandleJumpPhysics();
            _jumpPressedThisFrame = false;
        }

        public void SetRigidBody(Rigidbody rigidbody)
        {
            _rigidbody = rigidbody;
        }

        public void ResetRigidBody()
        {
            _rigidbody = null;
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
        }

        private void UpdateGroundedStatus()
        {
            var ray = new Ray(_rigidbody.position, -_movementDirectionProvider.Up);
            _isGrounded = Physics.Raycast(ray, out _, _settings.GroundCheckDistance, ~0,
                QueryTriggerInteraction.Ignore);
        }

        private void HandleMovement()
        {
            var normal = _movementDirectionProvider.Up;
            var forward = Vector3.ProjectOnPlane(_movementDirectionProvider.Forward, normal).normalized;
            var right = Vector3.Cross(normal, forward).normalized;

            var movementInput = _inputProvider.MovementInput;
            var desiredMove = forward * movementInput.y + right * movementInput.x;
            desiredMove = desiredMove.normalized * _settings.MoveSpeed;

            var velocity = _rigidbody.linearVelocity;

            var verticalVelocity = Vector3.Project(velocity, normal);
            var horizontalVelocity = velocity - verticalVelocity;

            if (_isGrounded)
            {
                horizontalVelocity = desiredMove;
            }
            else
            {
                horizontalVelocity =
                    Vector3.Lerp(horizontalVelocity, desiredMove, _settings.AirControl * Time.deltaTime);
            }

            _rigidbody.linearVelocity = horizontalVelocity + verticalVelocity;
        }

        private void HandleJumpPhysics()
        {
            var normal = _movementDirectionProvider.Up;
            var dt = Time.fixedDeltaTime;

            if (_jumpPressedThisFrame && _isGrounded)
            {
                _isJumping = true;
                _jumpHeld = true;
                _jumpTime = 0f;
                _rigidbody.linearVelocity -= Vector3.Project(_rigidbody.linearVelocity, normal);
                _rigidbody.AddForce(normal * _settings.InitialJumpForce, ForceMode.VelocityChange);
            }

            if (_isJumping)
            {
                if (_jumpHeld && _jumpTime < _settings.MaxJumpHoldTime)
                {
                    _rigidbody.AddForce(normal * (_settings.HoldJumpForce * dt), ForceMode.Acceleration);
                    _jumpTime += dt;
                }
                else
                {
                    _isJumping = false;
                }
            }
        }
    }
}