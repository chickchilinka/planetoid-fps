using Base.RigidbodyMovement;
using UnityEngine;

namespace Features.Character.Movement.Adapter
{
    [RequireComponent(typeof(Rigidbody))]
    public class LocalRigidbodyAdapter : MonoBehaviour, IRigidbody
    {
        private Rigidbody _rigidbody;

        private Rigidbody Rigidbody => _rigidbody ??= GetComponent<Rigidbody>();

        public Vector3 LinearVelocity
        {
            get => Rigidbody.linearVelocity;
            set => Rigidbody.linearVelocity = value;
        }

        public Vector3 Position
        {
            get => Rigidbody.position;
            set => Rigidbody.position = value;
        }

        public void AddForce(Vector3 force, ForceMode mode)
        {
            Rigidbody.AddForce(force, mode);
        }
    }
}