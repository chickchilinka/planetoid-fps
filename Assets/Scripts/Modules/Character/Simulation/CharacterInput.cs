using System;
using UnityEngine;

namespace Modules.Character.Simulation
{
    public readonly struct CharacterInput : IEquatable<CharacterInput>
    {
        public static readonly CharacterInput None = new CharacterInput(Vector2.zero, 0f, false, false);

        public CharacterInput(Vector2 move, float viewYaw, bool jumpPressed, bool jumpHeld)
        {
            Move = move;
            ViewYaw = viewYaw;
            JumpPressed = jumpPressed;
            JumpHeld = jumpHeld;
        }

        public Vector2 Move { get; }

        public float ViewYaw { get; }

        public bool JumpPressed { get; }

        public bool JumpHeld { get; }

        public bool Equals(CharacterInput other)
        {
            return Move.Equals(other.Move) &&
                   ViewYaw.Equals(other.ViewYaw) &&
                   JumpPressed == other.JumpPressed &&
                   JumpHeld == other.JumpHeld;
        }

        public override bool Equals(object obj)
        {
            return obj is CharacterInput other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Move.GetHashCode();
                hashCode = (hashCode * 397) ^ ViewYaw.GetHashCode();
                hashCode = (hashCode * 397) ^ JumpPressed.GetHashCode();
                hashCode = (hashCode * 397) ^ JumpHeld.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(CharacterInput left, CharacterInput right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CharacterInput left, CharacterInput right)
        {
            return !left.Equals(right);
        }
    }
}
