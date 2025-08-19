using System;
using System.Collections.Generic;
using Base.Network.Data;
using Base.Network.Model;
using Base.Network.Provider;
using Base.Network.Routing;
using UniRx;
using Zenject;

namespace Base.Network.Rule
{
    public class RouteMessagesFromClientsRule : IInitializable, IDisposable
    {
        private readonly INetworkServer _networkServer;
        private readonly ConnectionMessageRouter _connectionMessageRouter;

        private CompositeDisposable _connectionSubscriptions = new();
        private Dictionary<ConnectionId, IDisposable> _messageSubscriptions = new();

        public RouteMessagesFromClientsRule(INetworkServer networkServer, ConnectionMessageRouter connectionMessageRouter)
        {
            _networkServer = networkServer;
            _connectionMessageRouter = connectionMessageRouter;
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
            
            var subscription = connection.OnMessage.Subscribe(m => _connectionMessageRouter.RouteAsync(m, connection));
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