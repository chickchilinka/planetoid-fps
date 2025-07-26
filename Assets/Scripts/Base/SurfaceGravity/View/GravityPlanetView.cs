using System;
using System.Collections.Generic;
using Base.Common;
using Base.SurfaceGravity.Data;
using Base.SurfaceGravity.Services;
using Base.SurfaceGravity.Utils;
using UnityEngine;
using Zenject;

namespace Base.SurfaceGravity.View
{
    public class GravityPlanetView : MonoBehaviour, IIdentified
    {
        [SerializeField] private MeshCollider _gravityCollider;
        public string Id { get; } = Guid.NewGuid().ToString();
        public Collider Collider => _gravityCollider;

        private SurfaceGravityManagementService _surfaceGravity;
        private SurfaceGravitySettings _surfaceGravitySettings;

        private struct TriData
        {
            public Vector3 Centroid;
            public int I0, I1, I2;
        }

        private List<TriData> _triangles;
        private KDTree<TriData> _triangleIndex;

        private void BakeTriangleIndex(Mesh mesh)
        {
            var verts = mesh.vertices;
            var tris = mesh.triangles;
            _triangles = new List<TriData>(tris.Length / 3);

            for (var i = 0; i < tris.Length; i += 3)
            {
                var a = verts[tris[i]];
                var b = verts[tris[i + 1]];
                var c = verts[tris[i + 2]];
                var cent = (a + b + c) / 3f;
                _triangles.Add(new TriData { Centroid = cent, I0 = tris[i], I1 = tris[i + 1], I2 = tris[i + 2] });
            }

            _triangleIndex = new KDTree<TriData>(
                _triangles,
                tri => transform.TransformPoint(tri.Centroid)
            );
        }

        [Inject]
        internal void Construct(SurfaceGravityManagementService surfaceGravity,
            SurfaceGravitySettings surfaceGravitySettings)
        {
            _surfaceGravity = surfaceGravity;
            _surfaceGravitySettings = surfaceGravitySettings;
        }

        private void Awake()
        {
            if (!_gravityCollider)
                throw new MissingComponentException("No gravity collider was set");

            BakeTriangleIndex(_gravityCollider.sharedMesh);
            _gravityCollider.gameObject.layer = LayerMask.NameToLayer(_surfaceGravitySettings.SurfaceGravityLayerName);
            _surfaceGravity.RegisterPlanet(this);
        }

        private void OnDestroy()
        {
            _surfaceGravity.UnregisterPlanet(this);
        }

        internal Vector3 GetClosestPoint(Vector3 worldPos)
        {
            var mesh = _gravityCollider.sharedMesh;
            var candidates = _triangleIndex.QueryNearest(worldPos, 8);

            var bestSqr = float.MaxValue;
            var bestPt = worldPos;

            foreach (var tri in candidates)
            {
                var v0 = transform.TransformPoint(mesh.vertices[tri.I0]);
                var v1 = transform.TransformPoint(mesh.vertices[tri.I1]);
                var v2 = transform.TransformPoint(mesh.vertices[tri.I2]);
                
                var pt = ClosestPointOnTriangle(worldPos, v0, v1, v2);
                var sq = (pt - worldPos).sqrMagnitude;

                if (sq >= bestSqr) 
                    continue;
                
                bestSqr = sq;
                bestPt = pt;
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