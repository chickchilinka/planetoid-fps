using System.Collections.Generic;
using System.Linq;
using Base.SurfaceGravity.Data;
using Base.SurfaceGravity.Storage;
using Base.SurfaceGravity.View;
using UnityEngine;
using Zenject;

namespace Base.SurfaceGravity.Services
{
    public class SurfaceGravityService : IFixedTickable
    {
        private readonly GravityBodyViewStorage _bodyViewStorage;
        private readonly GravityBodyModelStorage _bodyModelStorage;
        private readonly GravityPlanetViewStorage _planetViewStorage;
        private readonly SurfaceGravitySettings _gravitySettings;

        private LayerMask _gravityLayerMask;

        internal SurfaceGravityService(GravityBodyViewStorage bodyView, GravityPlanetViewStorage viewStorage,
            SurfaceGravitySettings gravitySettings, GravityBodyModelStorage bodyModelStorage)
        {
            _bodyViewStorage = bodyView;
            _planetViewStorage = viewStorage;
            _gravitySettings = gravitySettings;
            _bodyModelStorage = bodyModelStorage;

            _gravityLayerMask = LayerMask.GetMask(_gravitySettings.SurfaceGravityLayerName);
        }

        public Vector3 GetBodyUp(string id)
        {
            if (!_bodyModelStorage.TryGetValue(id, out var bodyModel))
                throw new KeyNotFoundException($"No gravity body model found with id: {id}");

            return bodyModel.SmoothNormal;
        }

        public void FixedTick()
        {
            var dt = Time.fixedDeltaTime;

            foreach (var (id, body) in _bodyViewStorage.Dictionary)
            {
                var model = _bodyModelStorage.GetOrThrow(id);

                var origin = body.transform.position;
                var normal = (origin - model.CurrentClosestPoint).normalized;

                if (body.LerpUpToNormal && SampleGroundNormal(body.transform, out normal))
                {
                    model.SmoothNormal = Vector3.Slerp(
                        model.SmoothNormal,
                        normal,
                        _gravitySettings.NormalLerpSpeed * dt
                    );
                }
                else
                {
                    if (FindClosestPoint(origin, out var cp))
                    {
                        model.CurrentClosestPoint = cp!.Value;
                        normal = (origin - cp!.Value).normalized;
                    }
                }

                body.Rigidbody.AddForce(
                    -normal * _gravitySettings.GravityAcceleration,
                    ForceMode.Acceleration
                );

                if (!body.LerpUpToNormal)
                    continue;

                model.SmoothNormal = Vector3.Slerp(
                    model.SmoothNormal,
                    normal,
                    _gravitySettings.NormalLerpSpeed * dt
                );

                var target = Quaternion.FromToRotation(
                    body.transform.up,
                    model.SmoothNormal
                ) * body.Rigidbody.rotation;

                body.Rigidbody.MoveRotation(
                    Quaternion.RotateTowards(
                        body.Rigidbody.rotation,
                        target,
                        _gravitySettings.MaxRotateDeg
                    )
                );
            }
        }

        private bool FindClosestPoint(Vector3 position, out Vector3? closestPoint)
        {
            var maxHits = _planetViewStorage.Dictionary.Count;
            var buffer = new Collider[maxHits];
            var hitCount = Physics.OverlapSphereNonAlloc(
                position,
                _gravitySettings.PlanetSearchRadius,
                buffer,
                _gravityLayerMask,
                QueryTriggerInteraction.Ignore
            );
            
            closestPoint = null;

            GravityPlanetView bestPlanet = null;
            var bestSqr = float.MaxValue;

            for (var i = 0; i < hitCount; i++)
            {
                var col = buffer[i];
                var planet = _planetViewStorage.Dictionary.Values.FirstOrDefault(planet => planet.Collider == col);
                if (!planet)
                    continue;

                var cp = planet.GetClosestPoint(position);
                var sq = (cp - position).sqrMagnitude;
                if (sq >= bestSqr)
                    continue;
                
                bestSqr = sq;
                bestPlanet = planet;
                closestPoint = cp;
            }

            return bestPlanet;
        }

        private bool SampleGroundNormal(Transform transform, out Vector3 normal)
        {
            var offsets = new[]
            {
                Vector3.zero,
                transform.right * _gravitySettings.SphereCastRadius,
                -transform.right * _gravitySettings.SphereCastRadius,
                transform.forward * _gravitySettings.SphereCastRadius,
                -transform.forward * _gravitySettings.SphereCastRadius
            };

            var sum = Vector3.zero;
            var cnt = 0;

            foreach (var off in offsets)
            {
                if (!Physics.Raycast(
                        transform.position + off,
                        -transform.up,
                        out var hit,
                        _gravitySettings.RayLength,
                        _gravityLayerMask,
                        QueryTriggerInteraction.Ignore))
                    continue;
                sum += hit.normal;
                cnt++;
            }

            normal = cnt > 0
                ? (sum / cnt).normalized
                : transform.up;

            return cnt > 0;
        }
    }
}