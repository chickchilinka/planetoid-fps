using System;
using Base.Network.Provider;
using Base.Network.Routing;
using Cysharp.Threading.Tasks;
using UniRx;
using Zenject;

namespace Base.Network.Rule
{
    public class RouteMessagesFromServerRule : IInitializable, IDisposable
    {
        private readonly INetworkClient _networkClient;
        private readonly ClientMessageRouter _clientMessageRouter;
        private readonly ClientRequestRouter _clientRequestRouter;

        private IDisposable _subscription;

        public RouteMessagesFromServerRule(ClientMessageRouter clientMessageRouter, INetworkClient networkClient,
            ClientRequestRouter clientRequestRouter)
        {
            _clientMessageRouter = clientMessageRouter;
            _networkClient = networkClient;
            _clientRequestRouter = clientRequestRouter;
        }

        public void Initialize()
        {
            _subscription =
                _networkClient.OnMessage.Subscribe(message =>
                    _ = _clientRequestRouter.TryRouteAsync(message, reply => _networkClient.SendAsync(reply))
                        .ContinueWith(isHandled =>
                        {
                            if (!isHandled) 
                                _clientMessageRouter.RouteAsync(message).Forget();
                        })
                );
        }

        public void Dispose()
        {
            _subscription.Dispose();
        }
    }
}