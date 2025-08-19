using System;
using Base.Common;
using Base.SurfaceGravity.Services;
using Base.SurfaceGravity.Storage;
using UnityEngine;
using Zenject;

namespace Base.SurfaceGravity.View
{
    [RequireComponent(typeof(Rigidbody))]
    public class GravityBodyView : MonoBehaviour, IIdentified
    {
        [SerializeField] private bool _lerpUpToNormal;
        public bool LerpUpToNormal => _lerpUpToNormal;
        public string Id { get; } = Guid.NewGuid().ToString();
        public Quaternion Rotation => _rigidbody.rotation;

        private Rigidbody _rigidbody;

        private SurfaceGravityManagementService _gravityManagementService;

        [Inject]
        internal void Construct(SurfaceGravityManagementService gravityManagementService)
        {
            _gravityManagementService = gravityManagementService;
        }

        private void Awake()
        {
            _rigidbody ??= GetComponent<Rigidbody>();
            _rigidbody.useGravity = false;

            if (_lerpUpToNormal)
                _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;

            _gravityManagementService.RegisterBody(this);
        }

        private void OnDestroy()
        {
            _gravityManagementService.UnregisterBody(this);
        }

        public void AddForce(Vector3 force, ForceMode forceMode)
        {
            _rigidbody.AddForce(force, forceMode);
        }

        internal void MoveRotation(Quaternion rotation)
        {
            _rigidbody.MoveRotation(rotation);
        }
    }
}