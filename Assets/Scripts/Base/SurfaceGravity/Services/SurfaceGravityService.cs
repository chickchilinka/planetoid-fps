using System.Collections.Generic;
using System.Linq;
using Base.SurfaceGravity.Storage;
using Base.SurfaceGravity.Utils;
using Base.SurfaceGravity.View;
using UnityEngine;
using Zenject;

namespace Base.SurfaceGravity.Services
{
    public class SurfaceGravityService : IFixedTickable
    {
        private const float SphereRadius = 0.25f;
        private const float MaxRotateDeg = 45f;

        private readonly GravityBodyStorage _bodyStorage;
        private readonly GravityPlanetStorage _planetStorage;

        private class BodyState
        {
            public Vector3 smoothNormal;
            public Quaternion targetRot;
        }

        private readonly Dictionary<GravityBody, BodyState> _state = new();

        private readonly int _groundMask;

        public SurfaceGravityService(GravityBodyStorage b, GravityPlanetStorage p)
        {
            _bodyStorage = b;
            _planetStorage = p;
            _groundMask = LayerMask.GetMask(SurfaceGravityConst.SurfaceGravityLayerName);
        }


        public Vector3 GetBodyUp(Rigidbody body)
        {
            var gravityBody = _state.Keys.FirstOrDefault(gb => gb.Rb == body);
            if (!gravityBody)
                return body.transform.up;
            return _state[gravityBody].smoothNormal;
        }

        public void FixedTick()
        {
            var dt = Time.fixedDeltaTime;

            foreach (var body in _bodyStorage.Bodies)
            {
                if (!_state.TryGetValue(body, out var st))
                    _state[body] = st = new BodyState { smoothNormal = body.transform.up };

                var origin = body.transform.position;
                var down = -body.transform.up;

                if (Physics.SphereCast(origin,
                        SphereRadius,
                        down,
                        out var hit,
                        body.rayLength,
                        _groundMask,
                        QueryTriggerInteraction.Ignore))
                {
                    st.smoothNormal = Vector3.Slerp(st.smoothNormal,
                        hit.normal,
                        body.NormalLerpSpeed * dt);
                }

                body.Rb.AddForce(-st.smoothNormal *
                                 SurfaceGravityConst.GravityMultiplier,
                    ForceMode.Acceleration);

                var target = Quaternion.FromToRotation(body.transform.up,
                                 st.smoothNormal) *
                             body.Rb.rotation;

                body.Rb.MoveRotation(Quaternion.RotateTowards(body.Rb.rotation,
                    target,
                    MaxRotateDeg));
            }
        }
    }
}