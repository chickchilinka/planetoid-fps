using System.Collections.Generic;
using Base.SurfaceGravity.View;

namespace Base.SurfaceGravity.Storage
{
    public class GravityPlanetStorage
    {
        private readonly List<GravityPlanet> _items = new();
        public IReadOnlyList<GravityPlanet> Items => _items;

        public void Add(GravityPlanet p) => _items.Add(p);
        public void Remove(GravityPlanet p) => _items.Remove(p);
    }
}