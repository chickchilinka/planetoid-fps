using System.ComponentModel;
using Base.Network.Factory;
using Base.Network.Routing;
using Base.Network.Rule;
using Base.Network.Service;
using Base.Network.Storage;
using Zenject;

namespace Base.Network.Bootstrap
{
    public class NetworkBaseInstaller
    {
        public void InstallBindings(DiContainer container, bool isServer)
        {
            container.BindInterfacesAndSelfTo<RegisterGlobalHandlersRule>().AsSingle();
            container.BindInterfacesAndSelfTo<RegisterGlobalMessageTypesRule>().AsSingle();
            container.BindInterfacesAndSelfTo<NetworkService>().AsSingle().WithArguments(isServer);
            container.BindInterfacesAndSelfTo<MessageTypeRegistry>().AsSingle();
            container.BindInterfacesAndSelfTo<EnvelopeFactory>().AsSingle();
            
            container.Bind<MessageIdGenerator>().AsSingle();
            
            if (isServer)
                InstallServer(container);
            else 
                InstallClient(container);
        }

        private void InstallClient(DiContainer container)
        {
            container.Bind<ClientMessenger>().AsSingle();
            container.BindInterfacesAndSelfTo<ClientMessageRouter>().AsSingle();
            container.BindInterfacesAndSelfTo<RouteMessagesFromServerRule>().AsSingle();
        }

        private void InstallServer(DiContainer container)
        {
            container.Bind<ServerMessenger>().AsSingle();
            container.BindInterfacesAndSelfTo<ConnectionMessageRouter>().AsSingle();
            container.BindInterfacesAndSelfTo<RouteMessagesFromClientsRule>().AsSingle();
        }
    }
}