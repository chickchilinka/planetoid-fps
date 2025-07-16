using Base.Pool.Utils;
using Base.SurfaceGravity.Services;
using Base.SurfaceGravity.Storage;
using Zenject;

namespace Base.SurfaceGravity.Bootstrap
{
    public class SurfaceGravityMonoInstaller: MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.InstallAsSingle<GravityBodyStorage>();
            Container.InstallAsSingle<GravityPlanetStorage>();
            
            Container.InstallAsSingle<SurfaceGravityService>();
        }
    }
}