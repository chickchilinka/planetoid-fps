using System;
using Base.Network.Data;
using Cysharp.Threading.Tasks;

namespace Base.Network.Model
{
    public interface IConnection
    {
        ConnectionId Id { get; }
        IObservable<Envelope> OnMessage { get; }
        UniTask SendAsync(in Envelope envelope);
        UniTask DisconnectAsync(DisconnectReason reason);
    }
}