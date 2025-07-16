using System;
using UniRx;
using UnityEngine;

namespace Base.RigidbodyMovement.Providers
{
    public interface IMovementInputProvider
    {
        public Vector2 MovementInput { get; }
        public IReadOnlyReactiveProperty<bool> JumpInput { get; }
    }
}