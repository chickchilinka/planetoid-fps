using System;
using Base.Network.Provider;
using Base.Network.Routing;
using UniRx;
using Zenject;

namespace Base.Network.Rule
{
    public class RouteMessagesFromServerRule: IInitializable, IDisposable
    {
        private readonly INetworkClient _networkClient;
        private readonly ClientMessageRouter _connectionMessageRouter;

        private IDisposable _subscription;

        public RouteMessagesFromServerRule(ClientMessageRouter connectionMessageRouter, INetworkClient networkClient)
        {
            _connectionMessageRouter = connectionMessageRouter;
            _networkClient = networkClient;
        }

        public void Initialize()
        {
            _subscription = _networkClient.OnMessage.Subscribe(message=>_connectionMessageRouter.RouteAsync(message));
        }

        public void Dispose()
        {
            _subscription.Dispose();
        }
    }
}