using Base.SurfaceGravity.Storage;
using Base.SurfaceGravity.Utils;
using UnityEngine;
using Zenject;

namespace Base.SurfaceGravity.View
{
    public class GravityPlanet : MonoBehaviour
    {
        [SerializeField] private MeshCollider _gravityCollider;

        [field: SerializeField]
        [field: HideInInspector]
        public Vector3[] GravityPoints { get; private set; }

        private GravityPlanetStorage _storage;

        [Inject]
        public void Construct(GravityPlanetStorage storage)
        {
            _storage = storage;
        }

        private void Awake()
        {
            if(!_gravityCollider)
                throw new MissingComponentException("No gravity collider was set");
            
            _gravityCollider.gameObject.layer = LayerMask.NameToLayer(SurfaceGravityConst.SurfaceGravityLayerName);
            BakePoints();
            _storage.Add(this);
        }

        private void OnDestroy()
        {
            _storage.Remove(this);
        }

        [ContextMenu("Bake Points")]
        void BakePoints()
        {
            var mesh = _gravityCollider.sharedMesh;
            var verts = mesh.vertices;
            var tris = mesh.triangles;

            GravityPoints = new Vector3[tris.Length / 3];
            var j = 0;

            for (var i = 0; i < tris.Length; i += 3)
            {
                var p0 = transform.TransformPoint(verts[tris[i]]);
                var p1 = transform.TransformPoint(verts[tris[i + 1]]);
                var p2 = transform.TransformPoint(verts[tris[i + 2]]);
                GravityPoints[j++] = (p0 + p1 + p2) / 3f;
            }
        }
    }
}