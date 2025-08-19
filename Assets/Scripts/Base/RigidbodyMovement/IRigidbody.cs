using UnityEngine;

namespace Base.RigidbodyMovement
{
    public interface IRigidbody
    {
        public Vector3 LinearVelocity { get; set; }
        public Vector3 Position { get; set; }
        
        public void AddForce(Vector3 force, ForceMode mode);
    }
}