using System;
using Base.Common;
using Base.SurfaceGravity.Services;
using Base.SurfaceGravity.Storage;
using Base.SurfaceGravity.Utils;
using UnityEngine;
using Zenject;

namespace Base.SurfaceGravity.View
{
    public class GravityPlanet : MonoBehaviour, IIdentified
    {
        [SerializeField] private MeshCollider _gravityCollider;
        public string Id { get; } = Guid.NewGuid().ToString();
        
        private SurfaceGravityManagementService _surfaceGravity;

        [Inject]
        internal void Construct(SurfaceGravityManagementService surfaceGravity)
        {
            _surfaceGravity = surfaceGravity;
        }

        private void Awake()
        {
            if(!_gravityCollider)
                throw new MissingComponentException("No gravity collider was set");
            
            _gravityCollider.gameObject.layer = LayerMask.NameToLayer(SurfaceGravityConst.SurfaceGravityLayerName);
            _surfaceGravity.RegisterPlanet(this);
        }

        private void OnDestroy()
        {
            _surfaceGravity.UnregisterPlanet(this);
        }

        internal Vector3 GetClosestPoint(Vector3 position)
        {
            return _gravityCollider.ClosestPoint(position);
        }
    }
}