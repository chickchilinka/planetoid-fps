using System;
using Base.Network.Data;
using Base.Network.Model;
using Base.Network.Provider;
using Base.Network.Service;
using Cysharp.Threading.Tasks;
using UniRx;
using Zenject;

namespace Demos.NetworkingDemo.Scripts.Rule
{
    public class MessageOnConnectRule: IInitializable, IDisposable
    {
        private readonly INetworkServer _networkServer;
        private readonly ServerMessenger _serverMessenger;

        private IDisposable _subscription;
        public MessageOnConnectRule(INetworkServer networkServer, ServerMessenger serverMessenger)
        {
            _networkServer = networkServer;
            _serverMessenger = serverMessenger;
        }

        public void Initialize()
        {
            _networkServer.StartAsync(new ServerStartOptions()).Forget();
            _subscription = _networkServer.OnConnected.Subscribe(SendOnConnect);
        }

        private void SendOnConnect(IConnection connection)
        {
            _serverMessenger.To(connection.Id, new DemoMessage()
            {
                Message = "Welcome to the server!"
            });
        }

        public void Dispose()
        {
            _subscription?.Dispose();
        }
    }
}