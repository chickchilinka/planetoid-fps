using Base.Pool.Utils;
using Base.SurfaceGravity.Data;
using Base.SurfaceGravity.Services;
using Base.SurfaceGravity.Storage;
using UnityEngine;
using Zenject;

namespace Base.SurfaceGravity.Bootstrap
{
    public class SurfaceGravityMonoInstaller: MonoInstaller
    {
        [SerializeField]
        private SurfaceGravitySettings _gravitySettings;
        public override void InstallBindings()
        {
            Container.InstallAsSingle<GravityBodyModelStorage>();
            Container.InstallAsSingle<GravityBodyStorage>();
            Container.InstallAsSingle<GravityPlanetStorage>();
            
            Container.InstallAsSingle<SurfaceGravityService>();
            Container.InstallAsSingle<SurfaceGravityManagementService>();
            
            Container.InstallFromInstanceAsSingle(_gravitySettings);
        }
    }
}