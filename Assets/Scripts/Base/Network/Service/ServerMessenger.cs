using System.Collections.Generic;
using Base.Network.Data;
using Base.Network.Factory;
using Base.Network.Provider;
using Cysharp.Threading.Tasks;

namespace Base.Network.Service
{
    public class ServerMessenger
    {
        private readonly INetworkServer _server;
        private readonly EnvelopeFactory _envelopeFactory;

        public ServerMessenger(INetworkServer server, EnvelopeFactory envelopeFactory)
        {
            _server = server;
            _envelopeFactory = envelopeFactory;
        }

        public UniTask To<T>(ConnectionId to, in T msg) where T : struct, IMessagePayload
            => _server.BroadcastAsync(to, _envelopeFactory.Create(msg, to));

        public UniTask Broadcast<T>(in T msg, ConnectionId[] except = null) where T: struct, IMessagePayload
        {
            var env = _envelopeFactory.Create(msg, default);
            return except == null ? _server.BroadcastAsync(env) : _server.BroadcastAsync(env, except);
        }
    }
}