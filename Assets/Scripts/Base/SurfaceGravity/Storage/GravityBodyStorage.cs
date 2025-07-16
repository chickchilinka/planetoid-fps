using System.Collections.Generic;
using Base.SurfaceGravity.View;

namespace Base.SurfaceGravity.Storage
{
    public class GravityBodyStorage
    {
        readonly List<GravityBody> _bodies = new();
        public IReadOnlyList<GravityBody> Bodies => _bodies;

        public void Add(GravityBody b) => _bodies.Add(b);
        public void Remove(GravityBody b) => _bodies.Remove(b);
    }
}