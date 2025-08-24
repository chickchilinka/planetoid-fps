using System;
using System.Collections.Generic;
using System.Threading;
using Base.Network.Data;
using Base.Network.Factory;
using Base.Network.Provider;
using Cysharp.Threading.Tasks;

namespace Base.Network.Service
{
    public class ServerMessenger
    {
        private readonly INetworkServer _networkServer;
        private readonly EnvelopeFactory _envelopeFactory;
        private readonly ISerializer _serializer;

        private readonly Dictionary<uint, Action<Envelope>> _pendingRepliesByMessageId = new();

        public ServerMessenger(INetworkServer networkServer, EnvelopeFactory envelopeFactory, ISerializer serializer)
        {
            _networkServer = networkServer;
            _envelopeFactory = envelopeFactory;
            _serializer = serializer;
        }

        public UniTask To<T>(ConnectionId to, in T message) where T : struct, IMessagePayload
            => _networkServer.BroadcastAsync(to, _envelopeFactory.Create(message, to));

        public UniTask Broadcast<T>(in T message, ConnectionId[] except = null) where T : struct, IMessagePayload
        {
            var envelope = _envelopeFactory.Create(message, default);
            return except == null ? _networkServer.BroadcastAsync(envelope) : _networkServer.BroadcastAsync(envelope, except);
        }

        public async UniTask<TResponse> Request<TRequest, TResponse>(ConnectionId to, TRequest request, TimeSpan timeout)
            where TRequest : struct, IMessagePayload
            where TResponse : struct, IMessagePayload
        {
            var envelope = _envelopeFactory.Create(request, default);

            var taskCompletionSource = new UniTaskCompletionSource<TResponse>();
            using var cancellationTokenSource = new CancellationTokenSource(timeout);

            _pendingRepliesByMessageId[envelope.Id.Value] = replyEnvelope =>
            {
                try
                {
                    var response = _serializer.Deserialize<TResponse>(replyEnvelope.Payload);
                    taskCompletionSource.TrySetResult(response);
                }
                catch (Exception exception)
                {
                    taskCompletionSource.TrySetException(exception);
                }
            };

            using (cancellationTokenSource.Token.Register(() =>
            {
                _pendingRepliesByMessageId.Remove(envelope.Id.Value);
                taskCompletionSource.TrySetException(new TimeoutException());
            }))
            {
                await _networkServer.BroadcastAsync(to, envelope);
                return await taskCompletionSource.Task;
            }
        }

        public bool TryHandleReply(in Envelope envelope)
        {
            if (envelope.ReplyTo.Value == 0)
                return false;

            if (_pendingRepliesByMessageId.Remove(envelope.ReplyTo.Value, out var callback))
            {
                callback(envelope);
                return true;
            }

            return false;
        }
    }
}