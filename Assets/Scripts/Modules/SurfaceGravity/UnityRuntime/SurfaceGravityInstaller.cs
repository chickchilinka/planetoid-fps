using Modules.SurfaceGravity.Core;
using UnityEngine;
using Zenject;

namespace Modules.SurfaceGravity.UnityRuntime
{
    public sealed class SurfaceGravityInstaller : MonoInstaller
    {
        [Header("Scene query")]
        [SerializeField, Min(0.001f)] private float _searchRadius = 300f;
        [SerializeField, Min(1)] private int _maxOverlaps = 64;

        [Header("Solver")]
        [SerializeField, Min(0.001f)] private float _acceleration = 9.81f;
        [SerializeField, Min(0.001f)] private float _normalSharpness = 6f;
        [SerializeField, Min(0f)] private float _switchHysteresisSqr = 0.25f;

        public override void InstallBindings()
        {
            var views = FindObjectsByType<GravitySurfaceView>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            var settings = new SurfaceGravitySettings(
                _acceleration,
                _normalSharpness,
                _switchHysteresisSqr);

            Container.BindInstance(settings).AsSingle();
            Container.Bind<IGravitySurfaceProvider>()
                .To<SceneGravitySurfaceProvider>()
                .AsSingle()
                .WithArguments(views, _searchRadius, _maxOverlaps);
            Container.Bind<ISurfaceGravitySolver>()
                .To<SurfaceGravitySolver>()
                .AsSingle();
        }
    }
}
