using Base.Network.Bootstrap;
using Base.Network.Data;
using Base.Network.Provider;
using Base.Network.Routing;
using Base.Network.Rule;
using Base.Network.Service;
using Base.Network.Storage;
using Base.Network.Utils;
using Features.FishNetworking.Impl.Adapters;
using Features.FishNetworking.Impl.Data;
using Features.FishNetworking.Impl.Providers;
using Features.FishNetworking.Impl.Utils;
using FishNet.Managing;
using FishNet.Managing.Timing;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject;

namespace Features.FishNetworking.Bootstrap
{
    public class FishNetNetworkInstaller: MonoInstaller
    {
        [SerializeField]
        private NetworkManager _networkManager;

        [SerializeField] 
        private TimeManager _timeManager;

        [SerializeField] 
        private FishNetGeneralSettings _fishNetGeneralSettings;

        public override void InstallBindings()
        {
            new NetworkBaseInstaller().InstallBindings(Container, _fishNetGeneralSettings.IsServer);
            Container.Bind<NetworkManager>().FromInstance(_networkManager).AsSingle();
            Container.Bind<TimeManager>().FromInstance(_timeManager).AsSingle();
            Container.Bind<FishNetGeneralSettings>().FromInstance(_fishNetGeneralSettings).AsSingle();
            Container.BindInterfacesAndSelfTo<MessagePackNetworkSerializer>().AsSingle();
            Container.BindInterfacesAndSelfTo<FishnetTickSourceProvider>().AsSingle();
            
            if(_fishNetGeneralSettings.IsServer)
                InstallServer();
            else 
                InstallClient();

            var networkInstallers = transform.GetComponentsInChildren<AbstractNetworkMonoInstaller>();
            foreach (var installer in networkInstallers)
            {
                installer.Install(Container, _fishNetGeneralSettings.IsServer);
            }
        }

        private void InstallServer()
        {
            Container.BindInterfacesAndSelfTo<FishNetServerAdapter>().AsSingle();
        }

        private void InstallClient()
        {
            Container.BindInterfacesAndSelfTo<FishNetClientAdapter>().AsSingle();
        }
    }
}