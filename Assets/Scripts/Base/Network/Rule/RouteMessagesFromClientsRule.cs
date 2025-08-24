using System;
using System.Collections.Generic;
using Base.Network.Data;
using Base.Network.Model;
using Base.Network.Provider;
using Base.Network.Routing;
using Base.Network.Service;
using Cysharp.Threading.Tasks;
using UniRx;
using Zenject;

namespace Base.Network.Rule
{
    public class RouteMessagesFromClientsRule : IInitializable, IDisposable
    {
        private readonly INetworkServer _networkServer;
        private readonly ConnectionRequestRouter _connectionRequestRouter;
        private readonly ConnectionMessageRouter _notifyRouter;
        private readonly ServerMessenger _serverMessenger;

        private readonly CompositeDisposable _connectionSubscriptions = new();
        private readonly Dictionary<ConnectionId, IDisposable> _messageSubscriptions = new();

        public RouteMessagesFromClientsRule(INetworkServer networkServer,
            ConnectionMessageRouter connectionMessageRouter,
            ConnectionRequestRouter connectionRequestRouter, ServerMessenger serverMessenger)
        {
            _networkServer = networkServer;
            _notifyRouter = connectionMessageRouter;
            _connectionRequestRouter = connectionRequestRouter;
            _serverMessenger = serverMessenger;
        }

        public void Initialize()
        {
            _networkServer.OnConnected.Subscribe(SubscribeToMessages)
                .AddTo(_connectionSubscriptions);
            _networkServer.OnDisconnected.Subscribe(UnsubscribeFromMessages)
                .AddTo(_connectionSubscriptions);
        }

        private void UnsubscribeFromMessages((ConnectionId connectionId, DisconnectReason reason) data)
        {
            if (!_messageSubscriptions.TryGetValue(data.connectionId, out var subscription))
                return;

            subscription.Dispose();
            _messageSubscriptions.Remove(data.connectionId);
        }


        private void SubscribeToMessages(IConnection connection)
        {
            if (_messageSubscriptions.TryGetValue(connection.Id, out var old))
                old.Dispose();

            var subscription =
                connection.OnMessage.Subscribe(envelope =>
                    {
                        if (_serverMessenger.TryHandleReply(envelope))
                            return;
                        _ = _connectionRequestRouter.TryRouteAsync(
                                envelope,
                                connection.Id,
                                reply => _networkServer.BroadcastAsync(connection.Id, reply)
                            )
                            .ContinueWith(handled =>
                            {
                                if (!handled)
                                    _notifyRouter.RouteAsync(envelope, connection).Forget();
                            });
                    }
                );
            _messageSubscriptions[connection.Id] = subscription;
        }

        public void Dispose()
        {
            _connectionSubscriptions.Dispose();
            foreach (var subscription in _messageSubscriptions.Values)
            {
                subscription.Dispose();
            }

            _messageSubscriptions.Clear();
        }
    }
}