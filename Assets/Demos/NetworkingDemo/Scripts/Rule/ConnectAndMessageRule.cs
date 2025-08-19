using System;
using Base.Network.Data;
using Base.Network.Provider;
using Base.Network.Service;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace Demos.NetworkingDemo.Scripts.Rule
{
    public class ConnectAndMessageRule: IInitializable, IDisposable
    {
        private readonly INetworkClient _networkClient;
        private readonly ClientMessenger _clientMessenger;

        public ConnectAndMessageRule(INetworkClient networkClient,ClientMessenger clientMessenger)
        {
            _networkClient = networkClient; 
            _clientMessenger = clientMessenger;
        }

        public async void Initialize()
        {
            try
            {
                Debug.Log("Starting connecting to the server...");
                await _networkClient.ConnectAsync(new ClientConnectOptions()
                {
                    Host = "127.0.0.1",
                    Port = 7770,
                    Timeout = TimeSpan.FromSeconds(20)
                });
                Debug.Log("Successfully connected to server");

                await _clientMessenger.Send(new DemoMessage()
                {
                    Message = "Hi, sending first message"
                });
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public void Dispose()
        {
            _networkClient.DisconnectAsync(DisconnectReason.DisconnectCalled).Forget();
        }
    }
}