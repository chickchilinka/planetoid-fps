using System.Collections.Generic;
using System.Linq;
using Base.SurfaceGravity.Data;
using Base.SurfaceGravity.Model;
using Base.SurfaceGravity.Storage;
using Base.SurfaceGravity.Utils;
using Base.SurfaceGravity.View;
using UnityEngine;
using Zenject;

namespace Base.SurfaceGravity.Services
{
    public class SurfaceGravityService : IFixedTickable
    {
        private readonly GravityBodyStorage _bodyStorage;
        private readonly GravityBodyModelStorage _bodyModelStorage;
        private readonly GravityPlanetStorage _planetStorage;
        private readonly SurfaceGravitySettings _gravitySettings;

        private readonly int _groundMask;

        internal SurfaceGravityService(GravityBodyStorage body, GravityPlanetStorage storage,
            SurfaceGravitySettings gravitySettings, GravityBodyModelStorage bodyModelStorage)
        {
            _bodyStorage = body;
            _planetStorage = storage;
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

            foreach (var (id, body) in _bodyStorage.Dictionary)
            {
                if(!_bodyModelStorage.TryGetValue(id, out var model))
                    throw new KeyNotFoundException($"No gravity body model found with id: {id}");
                
                var origin = body.transform.position;
                var down = -body.transform.up;

                if (Physics.SphereCast(origin,
                        _gravitySettings.SphereRadius,
                        down,
                        out var hit,
                        _gravitySettings.RayLength,
                        _groundMask,
                        QueryTriggerInteraction.Ignore))
                {
                    model.SmoothNormal = Vector3.Slerp(model.SmoothNormal,
                        hit.normal,
                        _gravitySettings.NormalLerpSpeed * dt);
                }

                body.Rigidbody.AddForce(-model.SmoothNormal *
                                        _gravitySettings.GravityAcceleration,
                    ForceMode.Acceleration);

                var target = Quaternion.FromToRotation(body.transform.up,
                                 model.SmoothNormal) *
                             body.Rigidbody.rotation;

                body.Rigidbody.MoveRotation(Quaternion.RotateTowards(body.Rigidbody.rotation,
                    target,
                    _gravitySettings.MaxRotateDeg));
            }
        }
    }
}