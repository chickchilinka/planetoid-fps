using Base.Network.Utils;
using Demos.NetworkingDemo.Scripts.Rule;
using Zenject;

namespace Demos.NetworkingDemo.Scripts.Bootstrap
{
    public class NetworkingDemoInstaller : AbstractNetworkMonoInstaller
    {
        protected override void InstallCommon(DiContainer container)
        {
            container.RegisterMessageType<DemoMessage>(1);
            container.RegisterMessageType<DemoRequest>(2);
        }

        protected override void InstallClient(DiContainer container)
        {
            container.BindInterfacesAndSelfTo<ConnectAndMessageRule>().AsSingle();
            container.RegisterClientMessageHandler<DemoClientMessageHandler, DemoMessage>();
            container.RegisterClientRequestHandler<DemoClientRequestHandler, DemoRequest, DemoRequest>();
        }

        protected override void InstallServer(DiContainer container)
        {
            container.BindInterfacesAndSelfTo<MessageOnConnectRule>().AsSingle();
            container.RegisterConnectionMessageHandler<DemoServerMessageHandler, DemoMessage>();
            container.RegisterConnectionRequestHandler<DemoServerRequestHandler, DemoRequest, DemoRequest>();
        }
    }
}