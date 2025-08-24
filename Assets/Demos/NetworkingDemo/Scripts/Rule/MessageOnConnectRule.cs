using System;
using Base.Network.Data;
using Base.Network.Model;
using Base.Network.Provider;
using Base.Network.Service;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using Zenject;

namespace Demos.NetworkingDemo.Scripts.Rule
{
    public class MessageOnConnectRule : IInitializable, IDisposable
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

        private async void SendOnConnect(IConnection connection)
        {
            try
            {
                await _serverMessenger.To(connection.Id, new DemoMessage()
                {
                    Message = "Welcome to the server!"
                });

                var response = await _serverMessenger.Request<DemoRequest, DemoRequest>(connection.Id, new DemoRequest()
                {
                    Value = 10
                }, TimeSpan.FromSeconds(10));
                
                Debug.Log($"Client response is {response.Value}");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public void Dispose()
        {
            _subscription?.Dispose();
        }
    }
}