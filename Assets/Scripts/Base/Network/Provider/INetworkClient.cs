using System;
using Base.Network.Data;
using Cysharp.Threading.Tasks;
using UniRx;

namespace Base.Network.Provider
{
    public interface INetworkClient
    {
        IObservable<Envelope> OnMessage { get; }
        IObservable<Unit> OnConnected { get; }
        IObservable<DisconnectReason> OnDisconnected { get; }
        UniTask ConnectAsync(ClientConnectOptions opts);
        UniTask DisconnectAsync(DisconnectReason reason);
        UniTask SendAsync(in Envelope envelope);
    }
}