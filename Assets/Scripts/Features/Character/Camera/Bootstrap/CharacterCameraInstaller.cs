using Base.Pool.Utils;
using Base.ThirdPersonCamera.Data;
using Features.Character.Camera.Providers;
using UnityEngine;
using Zenject;

namespace Features.Character.Camera.Bootstrap
{
    public class CharacterCameraInstaller: MonoInstaller
    {
        [SerializeField]
        private ThirdPersonCameraSettings _cameraSettings;
        [SerializeField]
        private CameraTransformProvider _cameraTransformProvider;
        public override void InstallBindings()
        {
            Container.BindInstance(_cameraSettings).AsSingle();
            Container.BindInterfacesAndSelfTo<CameraInputProvider>().AsSingle();
            Container.InstallFromInstanceAsSingle(_cameraTransformProvider);
        }
    }
}