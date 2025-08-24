using System;
using System.Collections.Generic;
using Base.Network.Data;
using Base.Network.Factory;
using Base.Network.Handler;
using Base.Network.Provider;
using Base.Network.Storage;
using Cysharp.Threading.Tasks;

namespace Base.Network.Routing
{
    public class ClientRequestRouter
    {
        private readonly ISerializer _ser;
        private readonly MessageTypeRegistry _types;
        private readonly EnvelopeFactory _envelopeFactory;

        private readonly Dictionary<ushort, Func<Envelope, UniTask<Envelope?>>> _map = new();

        public ClientRequestRouter(ISerializer serializer, MessageTypeRegistry messageTypeRegistry, EnvelopeFactory envelopeFactory)
        {
            _ser = serializer;
            _types = messageTypeRegistry;
            _envelopeFactory = envelopeFactory;
        }

        public void Register<TReq, TRes>(IClientRequestHandler<TReq, TRes> handler)
            where TReq : struct, IMessagePayload
            where TRes : struct, IMessagePayload
        {
            var reqId = _types.GetId<TReq>();
            if (_map.ContainsKey(reqId))
                throw new InvalidOperationException($"Request type {reqId} is already registered.");

            var resRel = _types.GetDefaultReliability<TRes>();

            _map[reqId] = async (env) =>
            {
                if (env.ReplyTo.Value != 0)
                    return null;

                var dto = _ser.Deserialize<TReq>(env.Payload);
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
                    res = await handler.HandleAsync(dto, ctx);
                }
                catch
                {
                    return null;
                }
                
                var reply = _envelopeFactory.CreateReply(res, default, ctx.CorrelationId, resRel);
                return reply;
            };
        }

        public void Unregister<TReq, TRes>(IClientRequestHandler<TReq, TRes> _)
            where TReq : struct, IMessagePayload
            where TRes : struct, IMessagePayload
        {
            var reqId = _types.GetId<TReq>();
            _map.Remove(reqId);
        }
        
        public async UniTask<bool> TryRouteAsync(Envelope env, Func<Envelope, UniTask> send)
        {
            if (!_map.TryGetValue(env.Type, out var f))
                return false;

            var maybe = await f(env);
            if (maybe.HasValue)
                await send(maybe.Value);
            return true;
        }
    }
}