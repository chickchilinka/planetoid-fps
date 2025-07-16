using UnityEngine;

namespace Base.RigidbodyMovement.Providers
{
    public interface IMovementDirectionProvider
    {
        Vector3 Up { get; }
        Vector3 Forward { get; }
    }
}