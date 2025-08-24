using System;
using System.Collections.Generic;
using Base.Network.Data;
using Base.Network.Factory;
using Base.Network.Handler;
using Base.Network.Provider;
using Base.Network.Service;
using Base.Network.Storage;
using Cysharp.Threading.Tasks;

namespace Base.Network.Routing
{
    public class ConnectionRequestRouter
    {
        private readonly ISerializer _serializer;
        private readonly MessageTypeRegistry _messageTypeRegistry;
        private readonly EnvelopeFactory _envelopeFactory;
        
        private readonly Dictionary<ushort, Func<Envelope, ConnectionId, UniTask<Envelope?>>> _map = new();

        public ConnectionRequestRouter(ISerializer serializer, MessageTypeRegistry messageTypeRegistry, EnvelopeFactory envelopeFactory)
        {
            _serializer = serializer;
            _messageTypeRegistry = messageTypeRegistry;
            _envelopeFactory = envelopeFactory;
        }

        public void Register<TReq,TRes>(IConnectionRequestHandler<TReq,TRes> h)
            where TReq : struct, IMessagePayload
            where TRes : struct, IMessagePayload
        {
            var reqId = _messageTypeRegistry.GetId<TReq>();
            if (_map.ContainsKey(reqId))
                throw new InvalidOperationException($"Request type {reqId} is already registered.");

            var resRel = _messageTypeRegistry.GetDefaultReliability<TRes>();

            _map[reqId] = async (env, from) =>
            {
                if (env.ReplyTo.Value != 0) 
                    return null;

                var dto = _serializer.Deserialize<TReq>(env.Payload);
                var ctx = new MessageContext
                {
                    ServerTick    = env.ServerTick,
                    ClientTick    = env.ClientTick,
                    Source        = env.Source,
                    CorrelationId = env.Id
                };

                TRes res;
                try
                {
                    res = await h.HandleAsync(dto, from, ctx);
                }
                catch
                {
                    return null;
                }
                
                var reply = _envelopeFactory.CreateReply(res, from, ctx.CorrelationId, resRel);
                return reply;
            };
        }

        public void Unregister<TReq,TRes>(IConnectionRequestHandler<TReq,TRes> _)
            where TReq : struct, IMessagePayload
            where TRes : struct, IMessagePayload
        {
            var reqId = _messageTypeRegistry.GetId<TReq>();
            _map.Remove(reqId);
        }
        
        public async UniTask<bool> TryRouteAsync(Envelope env, ConnectionId from, Func<Envelope, UniTask> send)
        {
            if (!_map.TryGetValue(env.Type, out var f))
                return false;

            var maybe = await f(env, from);
            if (maybe.HasValue)
                await send(maybe.Value);
            return true;
        }
    }
}