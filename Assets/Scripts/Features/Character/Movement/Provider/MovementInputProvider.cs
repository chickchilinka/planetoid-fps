using Base.Input.Services;
using Base.RigidbodyMovement.Providers;
using UniRx;
using UnityEngine;
using Zenject;

namespace Features.Character.Movement.Provider
{
    public class MovementInputProvider : IMovementInputProvider, ITickable
    {
        public Vector2 MovementInput => new(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        public IReadOnlyReactiveProperty<bool> JumpInput => _jumpInput;

        private ReactiveProperty<bool> _jumpInput = new(false);


        private readonly InputService _inputService;

        public MovementInputProvider(InputService inputService)
        {
            _inputService = inputService;
        }

        public void Tick()
        {
            _jumpInput.Value = _inputService.GetButton("Jump");
        }
    }
}