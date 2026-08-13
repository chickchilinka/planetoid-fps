using System;
using System.Threading;
using System.Threading.Tasks;
using Base.Network.Data;
using Base.Network.Provider;
using Cysharp.Threading.Tasks;
using Modules.Multiplayer.Session;
using UniRx;

namespace Modules.Multiplayer.Session.Networking
{
    /// <summary>Adapts a session's opaque connection token to the live server connection for timeout recovery.</summary>
    public sealed class ServerSessionConnectionProvider : ISessionConnectionProvider, IDisposable
    {
        private readonly INetworkServer _server;
        private readonly SessionConnectionRegistry _registry;
        private readonly IDisposable _connections;
        private readonly IDisposable _disconnections;
        private readonly System.Collections.Generic.Dictionary<int, Base.Network.Model.IConnection> _byId = new();

        public ServerSessionConnectionProvider(INetworkServer server, SessionConnectionRegistry registry)
        {
            _server = server ?? throw new ArgumentNullException(nameof(server));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _connections = _server.OnConnected.Subscribe(connection => _byId[connection.Id.Value] = connection);
            _disconnections = _server.OnDisconnected.Subscribe(value => _byId.Remove(value.Item1.Value));
        }

        public async ValueTask DisconnectAsync(SessionConnection connection, SessionError reason, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (!_registry.TryGetTransport(connection, out var transport) ||
                !_byId.TryGetValue(transport.Value, out var liveConnection))
                return;

            await liveConnection.DisconnectAsync(DisconnectReason.ClosedByServer);
        }

        public void Dispose()
        {
            _connections.Dispose();
            _disconnections.Dispose();
            _byId.Clear();
        }
    }
}
