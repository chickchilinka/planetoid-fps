using Base.Common;
using UnityEngine;

namespace Base.SurfaceGravity.Model
{
    internal class GravityBodyModel: IIdentified
    {
        public string Id { get; }
        public string PlanetId { get; internal set; }
        public Vector3 SmoothNormal { get; internal set; }
        public Quaternion TargetRot { get; internal set; }

        public GravityBodyModel(string id)
        {
            Id = id;
        }
    }
}