using Base.Network.Rule;
using Base.Network.Service;
using Base.Network.Utils;
using Zenject;

namespace Features.FishNetworking.Bootstrap
{
    public class FishNetSceneInstaller: MonoInstaller
    {
        private NetworkService _networkService;

        [Inject]
        public void Construct(NetworkService networkService)
        {
            _networkService = networkService;
        }

        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<RegisterSceneHandlersRule>().AsSingle();
            Container.BindInterfacesAndSelfTo<RegisterSceneMessageTypesRule>().AsSingle();
            Container.BindExecutionOrder<RegisterSceneMessageTypesRule>(-100);
            Container.BindExecutionOrder<RegisterSceneHandlersRule>(-50);
            
            var networkInstallers = transform.GetComponentsInChildren<AbstractNetworkMonoInstaller>();
            foreach (var installer in networkInstallers)
            {
                installer.Install(Container, _networkService.IsServer);
            }
        }
    }
}