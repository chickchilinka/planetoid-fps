using System;
using Base.Network.Data;
using Base.Network.Model;
using Cysharp.Threading.Tasks;
using Features.FishNetworking.Impl.Utils;
using FishNet.Connection;
using FishNet.Managing.Server;
using UniRx;

namespace Features.FishNetworking.Impl.Models
{
    public sealed class FishNetConnection : IConnection, IDisposable
    {
        private readonly ServerManager _server;
        private readonly NetworkConnection _conn;
        private readonly Subject<Envelope> _onMessage = new();

        public FishNetConnection(ServerManager server, NetworkConnection conn)
        {
            _server = server ?? throw new ArgumentNullException(nameof(server));
            _conn = conn ?? throw new ArgumentNullException(nameof(conn));
            Id = new ConnectionId(_conn.ClientId);
        }

        public ConnectionId Id { get; }

        public IObservable<Envelope> OnMessage => _onMessage;

        public UniTask SendAsync(in Envelope envelope)
        {
            var channel = FishNetEnvelope.ChannelFromReliability(envelope.Reliability);
            var raw = FishNetEnvelope.ToRaw(envelope);
            _server.Broadcast(_conn, raw, requireAuthenticated:false, channel: channel);
            return UniTask.CompletedTask;
        }

        public UniTask DisconnectAsync(DisconnectReason reason)
        {
            _conn.Disconnect(true);
            return UniTask.CompletedTask;
        }

        internal void Push(in Envelope env) => _onMessage.OnNext(env);

        public void Dispose() => _onMessage.OnCompleted();
    }
}