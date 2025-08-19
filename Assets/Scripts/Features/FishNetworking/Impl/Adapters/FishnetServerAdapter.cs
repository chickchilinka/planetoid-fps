using System;
using System.Collections.Generic;
using Base.Network.Data;
using Base.Network.Model;
using Base.Network.Provider;
using Cysharp.Threading.Tasks;
using Features.FishNetworking.Impl.Data;
using Features.FishNetworking.Impl.Models;
using Features.FishNetworking.Impl.Utils;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Server;
using FishNet.Transporting;
using UniRx;
using Zenject;
using Channel = FishNet.Transporting.Channel;

namespace Features.FishNetworking.Impl.Adapters
{
    public sealed class FishNetServerAdapter : INetworkServer, IDisposable
    {
        private readonly NetworkManager _networkManager;
        private readonly Subject<IConnection> _onConnected = new();
        private readonly Subject<(ConnectionId, DisconnectReason)> _onDisconnected = new();
        private readonly Dictionary<NetworkConnection, FishNetConnection> _connectionMap = new();
        private ServerManager Server => _networkManager.ServerManager;

        public FishNetServerAdapter(NetworkManager networkManager)
        {
            _networkManager = networkManager;
        }

        public IObservable<IConnection> OnConnected => _onConnected;
        public IObservable<(ConnectionId, DisconnectReason)> OnDisconnected => _onDisconnected;

        public async UniTask StartAsync(ServerStartOptions opts)
        {
            Server.OnRemoteConnectionState += OnRemoteState;
            Server.RegisterBroadcast<RawEnvelope>(OnServerReceive);
            Server.StartConnection();
            await UniTask.CompletedTask;
        }

        public async UniTask StopAsync()
        {
            Server.OnRemoteConnectionState -= OnRemoteState;
            Server.UnregisterBroadcast<RawEnvelope>(OnServerReceive);
            Server.StopConnection(true);
            await UniTask.CompletedTask;
        }

        public UniTask BroadcastAsync(in Envelope envelope, ReadOnlySpan<ConnectionId> except = default)
        {
            var raw = FishNetEnvelope.ToRaw(envelope);
            var ch = FishNetEnvelope.ChannelFromReliability(envelope.Reliability);
            if (except.Length == 0)
            {
                Server.Broadcast(raw, channel: ch);
                return UniTask.CompletedTask;
            }

            foreach (var (nc, wrap) in _connectionMap)
                if (!Contains(except, wrap.Id.Value))
                    Server.Broadcast(nc, raw, requireAuthenticated:false, channel: ch);

            return UniTask.CompletedTask;

            static bool Contains(ReadOnlySpan<ConnectionId> s, int id)
            {
                for (var i = 0; i < s.Length; i++)
                    if (s[i].Value == id)
                        return true;
                return false;
            }
        }

        public UniTask BroadcastAsync(ConnectionId to, in Envelope envelope)
        {
            var raw = FishNetEnvelope.ToRaw(envelope);
            var ch = FishNetEnvelope.ChannelFromReliability(envelope.Reliability);
            foreach (var (nconn, wrap) in _connectionMap)
                if (wrap.Id.Value == to.Value)
                    Server.Broadcast(nconn, raw, requireAuthenticated: false, channel: ch);
            return UniTask.CompletedTask;
        }

        private void OnRemoteState(NetworkConnection connection, RemoteConnectionStateArgs args)
        {
            if (args.ConnectionState == RemoteConnectionState.Started)
            {
                var wrapper = new FishNetConnection(Server, connection);
                _connectionMap[connection] = wrapper;
                _onConnected.OnNext(wrapper);
            }
            else if (args.ConnectionState == RemoteConnectionState.Stopped)
            {
                if (!_connectionMap.Remove(connection, out var wrapper))
                    return;

                _onDisconnected.OnNext((wrapper.Id, DisconnectReason.DisconnectCalled));
                wrapper.Dispose();
            }
        }

        private void OnServerReceive(NetworkConnection conn, RawEnvelope raw, Channel channel)
        {
            if (!_connectionMap.TryGetValue(conn, out var wrapper))
                return;
            var env = FishNetEnvelope.FromRaw(raw, channel, conn);
            wrapper.Push(env);
        }

        public void Dispose()
        {
            Server.OnRemoteConnectionState -= OnRemoteState;
            Server.UnregisterBroadcast<RawEnvelope>(OnServerReceive);
            foreach (var connection in _connectionMap.Values)
                connection.Dispose();
            _connectionMap.Clear();
        }
    }
}