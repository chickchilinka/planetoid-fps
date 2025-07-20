using System.Collections.Generic;
using Base.SurfaceGravity.Data;
using Base.SurfaceGravity.Storage;
using Base.SurfaceGravity.Utils;
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

        private readonly int _groundMask;

        internal SurfaceGravityService(GravityBodyViewStorage bodyView, GravityPlanetViewStorage viewStorage,
            SurfaceGravitySettings gravitySettings, GravityBodyModelStorage bodyModelStorage)
        {
            _bodyViewStorage = bodyView;
            _planetViewStorage = viewStorage;
            _gravitySettings = gravitySettings;
            _bodyModelStorage = bodyModelStorage;
            _groundMask = LayerMask.GetMask(SurfaceGravityConst.SurfaceGravityLayerName);
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
                var down = -body.transform.up;

                if (body.LerpUpToNormal && Physics.SphereCast(origin,
                        _gravitySettings.SphereRadius,
                        down,
                        out var hit,
                        _gravitySettings.RayLength,
                        _groundMask,
                        QueryTriggerInteraction.Ignore))
                {
                    model.SmoothNormal = Vector3.Slerp(
                        model.SmoothNormal,
                        hit.normal,
                        _gravitySettings.NormalLerpSpeed * dt
                    );
                }
                else
                {
                    var planet = FindClosestPlanet(origin);
                    if (planet != null)
                    {
                        var cp = planet.GetClosestPoint(origin);
                        var dir = (origin - cp).normalized;

                        model.SmoothNormal = Vector3.Slerp(
                            model.SmoothNormal,
                            dir,
                            _gravitySettings.NormalLerpSpeed * dt
                        );
                    }
                }

                body.Rigidbody.AddForce(
                    -model.SmoothNormal * _gravitySettings.GravityAcceleration,
                    ForceMode.Acceleration
                );

                if (!body.LerpUpToNormal)
                    continue;

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

        private GravityPlanetView FindClosestPlanet(Vector3 pos)
        {
            GravityPlanetView best = null;
            var bestSqrMagnitude = float.MaxValue;
            var searchRadius = _gravitySettings.PlanetSearchRadius;

            foreach (var planet in _planetViewStorage.Dictionary.Values)
            {
                var sqrMagnitude = (planet.transform.position - pos).sqrMagnitude;

                if (sqrMagnitude > searchRadius * searchRadius)
                    continue;

                if (sqrMagnitude >= bestSqrMagnitude)
                    continue;

                bestSqrMagnitude = sqrMagnitude;
                best = planet;
            }

            return best;
        }
    }
}