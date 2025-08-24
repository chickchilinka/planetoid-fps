using System;
using System.Collections.Generic;
using Base.Network.Data;
using Base.Network.Factory;
using Base.Network.Provider;
using Cysharp.Threading.Tasks;
using UniRx;
using Zenject;

namespace Base.Network.Service
{
    public sealed class ClientMessenger : IInitializable, IDisposable
    {
        private readonly ISerializer _serializer;
        private readonly INetworkClient _client;
        private readonly EnvelopeFactory _envelopeFactory;

        private readonly Dictionary<uint, Action<Envelope>> _pending = new();
        private IDisposable _messageSubscription;

        public ClientMessenger(INetworkClient client, EnvelopeFactory envelopeFactory, ISerializer serializer)
        {
            _client = client;
            _envelopeFactory = envelopeFactory;
            _serializer = serializer;
        }

        public UniTask Send<T>(in T msg) where T : struct, IMessagePayload
            => _client.SendAsync(_envelopeFactory.Create(msg, default));

        public async UniTask<TRes> Request<TReq, TRes>(TReq req, TimeSpan timeout)
            where TReq : struct, IMessagePayload
            where TRes : struct, IMessagePayload
        {
            var e = _envelopeFactory.Create(req, default);

            var tcs = new UniTaskCompletionSource<TRes>();
            using var cts = new System.Threading.CancellationTokenSource(timeout);

            _pending[e.Id.Value] = (env) =>
            {
                try
                {
                    tcs.TrySetResult(_serializer.Deserialize<TRes>(env.Payload));
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            };

            using (cts.Token.Register(() =>
                         {
                             _pending.Remove(e.Id.Value);
                             tcs.TrySetException(new TimeoutException());
                         }))
            {
                await _client.SendAsync(e);
                return await tcs.Task;
            }
        }

        private void OnMessage(Envelope env)
        {
            if (env.ReplyTo.Value != 0 && _pending.Remove(env.ReplyTo.Value, out var cb))
                cb(env);
        }

        public void Initialize()
        {
            _messageSubscription = _client.OnMessage.Subscribe(OnMessage);
        }

        public void Dispose()
        {
            _messageSubscription?.Dispose();
        }
    }
}