using Base.SurfaceGravity.Model;
using Base.SurfaceGravity.Storage;
using Base.SurfaceGravity.View;

namespace Base.SurfaceGravity.Services
{
    internal class SurfaceGravityManagementService
    {
        private readonly GravityBodyViewStorage _gravityBodiesView;
        private readonly GravityBodyModelStorage _gravityBodyModels;
        private readonly GravityPlanetViewStorage _gravityPlanetsView;

        public SurfaceGravityManagementService(GravityBodyViewStorage gravityBodiesView, GravityBodyModelStorage gravityBodyModel,
            GravityPlanetViewStorage gravityPlanetsView)
        {
            _gravityBodiesView = gravityBodiesView;
            _gravityBodyModels = gravityBodyModel;
            _gravityPlanetsView = gravityPlanetsView;
        }

        internal void RegisterBody(GravityBodyView bodyView)
        {
            var model = new GravityBodyModel(bodyView.Id)
            {
                SmoothNormal = bodyView.transform.up
            };
            _gravityBodyModels.Add(model.Id, model);
            _gravityBodiesView.Add(bodyView.Id, bodyView);
        }

        internal void RegisterPlanet(GravityPlanetView planetView)
        {
            _gravityPlanetsView.Add(planetView.Id, planetView);
        }

        internal void UnregisterBody(GravityBodyView bodyView)
        {
            _gravityBodiesView.Remove(bodyView.Id);
            _gravityBodyModels.Remove(bodyView.Id);
        }

        internal void UnregisterPlanet(GravityPlanetView planetView)
        {
            _gravityPlanetsView.Remove(planetView.Id);
        }
    }
}