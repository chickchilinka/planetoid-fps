using Base.SurfaceGravity.Model;
using Base.SurfaceGravity.Storage;
using Base.SurfaceGravity.View;

namespace Base.SurfaceGravity.Services
{
    internal class SurfaceGravityManagementService
    {
        private readonly GravityBodyStorage _gravityBodies;
        private readonly GravityBodyModelStorage _gravityBodyModels;
        private readonly GravityPlanetStorage _gravityPlanets;

        public SurfaceGravityManagementService(GravityBodyStorage gravityBodies, GravityBodyModelStorage gravityBodyModel,
            GravityPlanetStorage gravityPlanets)
        {
            _gravityBodies = gravityBodies;
            _gravityBodyModels = gravityBodyModel;
            _gravityPlanets = gravityPlanets;
        }

        internal void RegisterBody(GravityBody body)
        {
            var model = new GravityBodyModel(body.Id)
            {
                SmoothNormal = body.transform.up
            };
            _gravityBodyModels.Add(model.Id, model);
            _gravityBodies.Add(body.Id, body);
        }

        internal void RegisterPlanet(GravityPlanet planet)
        {
            _gravityPlanets.Add(planet.Id, planet);
        }

        internal void UnregisterBody(GravityBody body)
        {
            _gravityBodies.Remove(body.Id);
            _gravityBodyModels.Remove(body.Id);
        }

        internal void UnregisterPlanet(GravityPlanet planet)
        {
            _gravityPlanets.Remove(planet.Id);
        }
    }
}