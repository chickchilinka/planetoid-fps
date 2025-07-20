using Base.Common;
using UnityEngine;

namespace Base.SurfaceGravity.Model
{
    internal class GravityBodyModel: IIdentified
    {
        public string Id { get; }
        public Vector3 SmoothNormal { get; internal set; }

        public GravityBodyModel(string id)
        {
            Id = id;
        }
    }
}