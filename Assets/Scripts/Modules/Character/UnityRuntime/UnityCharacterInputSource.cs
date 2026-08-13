using Modules.Character.Simulation;
using UnityEngine;
using Zenject;

namespace Modules.Character.UnityRuntime
{
    public sealed class UnityCharacterInputSource : ICharacterInputSource, ITickable
    {
        private readonly ICharacterViewYawProvider _viewYawProvider;

        private bool _previousJumpHeld;
        private bool _jumpPressedBuffered;
        private Vector2 _move;
        private float _viewYaw;

        public UnityCharacterInputSource(ICharacterViewYawProvider viewYawProvider)
        {
            _viewYawProvider = viewYawProvider ??
                               throw new System.ArgumentNullException(nameof(viewYawProvider));
        }

        public void Tick()
        {
            Sample(
                new Vector2(
                    Input.GetAxisRaw("Horizontal"),
                    Input.GetAxisRaw("Vertical")),
                _viewYawProvider.Yaw,
                Input.GetButton("Jump"));
        }

        public CharacterInput ConsumeForTick()
        {
            var input = new CharacterInput(
                _move,
                _viewYaw,
                _jumpPressedBuffered,
                _previousJumpHeld);
            _jumpPressedBuffered = false;
            return input;
        }

        internal void Sample(Vector2 move, float viewYaw, bool jumpHeld)
        {
            _jumpPressedBuffered |= jumpHeld && !_previousJumpHeld;
            _previousJumpHeld = jumpHeld;
            _move = Vector2.ClampMagnitude(move, 1f);
            _viewYaw = viewYaw;
        }
    }
}
