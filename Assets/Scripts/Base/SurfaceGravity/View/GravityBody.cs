using System;
using Base.Common;
using Base.SurfaceGravity.Services;
using Base.SurfaceGravity.Storage;
using UnityEngine;
using Zenject;

namespace Base.SurfaceGravity.View
{
    [RequireComponent(typeof(Rigidbody))]
    public class GravityBody : MonoBehaviour, IIdentified
    {
        [SerializeField] private bool _lerpUpToNormal;
        public bool LerpUpToNormal => _lerpUpToNormal;
        public string Id { get; } = Guid.NewGuid().ToString();

        public Rigidbody Rigidbody => _rigidbody ??= GetComponent<Rigidbody>();

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
    }
}