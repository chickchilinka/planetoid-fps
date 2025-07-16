using Base.SurfaceGravity.Storage;
using UnityEngine;
using Zenject;

namespace Base.SurfaceGravity.View
{
    [RequireComponent(typeof(Rigidbody))]
    public class GravityBody : MonoBehaviour
    {
        [Header("Ray")] 
        public float rayLength = 3f;

        [Header("Smoothing")] 
        public float NormalLerpSpeed = 10f;

        public Rigidbody Rb { get; private set; }

        private GravityBodyStorage _storage;

        [Inject]
        public void Construct(GravityBodyStorage storage)
        {
            _storage = storage;
        }

        private void Awake()
        {
            Rb = GetComponent<Rigidbody>();
            Rb.useGravity = false;
            Rb.constraints = RigidbodyConstraints.FreezeRotation;
            _storage.Add(this);
        }

        private void OnDestroy()
        {
            _storage.Remove(this);
        }
    }
}