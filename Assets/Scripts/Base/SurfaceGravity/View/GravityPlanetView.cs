using System;
using Base.Common;
using Base.SurfaceGravity.Services;
using Base.SurfaceGravity.Storage;
using Base.SurfaceGravity.Utils;
using UnityEngine;
using Zenject;

namespace Base.SurfaceGravity.View
{
    public class GravityPlanetView : MonoBehaviour, IIdentified
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
            if (!_gravityCollider)
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
            var mesh = _gravityCollider.sharedMesh;
            var verts = mesh.vertices;
            var tris = mesh.triangles;

            var bestSqr = float.MaxValue;
            var bestPt = position;

            for (var i = 0; i < tris.Length; i += 3)
            {
                var v0 = transform.TransformPoint(verts[tris[i]]);
                var v1 = transform.TransformPoint(verts[tris[i + 1]]);
                var v2 = transform.TransformPoint(verts[tris[i + 2]]);
                
                var pt = ClosestPointOnTriangle(position, v0, v1, v2);

                var sqr = (pt - position).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    bestPt = pt;
                }
            }

            return bestPt;
        }

        private static Vector3 ClosestPointOnTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            var ab = b - a;
            var ac = c - a;
            var ap = p - a;
            var d1 = Vector3.Dot(ab, ap);
            var d2 = Vector3.Dot(ac, ap);
            if (d1 <= 0f && d2 <= 0f) return a;
            
            var bp = p - b;
            var d3 = Vector3.Dot(ab, bp);
            var d4 = Vector3.Dot(ac, bp);
            if (d3 >= 0f && d4 <= d3) return b;
            
            var vc = d1 * d4 - d3 * d2;
            if (vc <= 0f && d1 >= 0f && d3 <= 0f)
            {
                var v = d1 / (d1 - d3);
                return a + v * ab;
            }
            
            var cp = p - c;
            var d5 = Vector3.Dot(ab, cp);
            var d6 = Vector3.Dot(ac, cp);
            if (d6 >= 0f && d5 <= d6) return c;
            
            var vb = d5 * d2 - d1 * d6;
            if (vb <= 0f && d2 >= 0f && d6 <= 0f)
            {
                var w = d2 / (d2 - d6);
                return a + w * ac;
            }
            
            var va = d3 * d6 - d5 * d4;
            if (va <= 0f && (d4 - d3) >= 0f && (d5 - d6) >= 0f)
            {
                var bc = c - b;
                var w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                return b + w * bc;
            }
            
            var denom = 1f / (va + vb + vc);
            var v2 = vb * denom;
            var w2 = vc * denom;
            return a + ab * v2 + ac * w2;
        }
    }
}