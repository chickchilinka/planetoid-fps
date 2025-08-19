using System;
using Base.Network.Data;
using Base.Network.Model;
using Cysharp.Threading.Tasks;

namespace Base.Network.Provider
{
    public interface INetworkServer
    {
        IObservable<IConnection> OnConnected { get; }
        IObservable<(ConnectionId, DisconnectReason)> OnDisconnected { get; }
        
        UniTask StartAsync(ServerStartOptions opts);
        UniTask StopAsync();
        UniTask BroadcastAsync(in Envelope envelope, ReadOnlySpan<ConnectionId> except = default);
        UniTask BroadcastAsync(ConnectionId to, in Envelope envelope);
    }
}