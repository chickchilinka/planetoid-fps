using System;
using System.Collections.Generic;
using Base.Network.Data;
using Base.Network.Handler;
using Base.Network.Provider;
using Base.Network.Storage;
using Cysharp.Threading.Tasks;

namespace Base.Network.Routing
{
    public class ClientMessageRouter
    {
        private readonly ISerializer _ser;
        private readonly MessageTypeRegistry _messageTypeRegistry;
        private readonly Dictionary<ushort, Dictionary<object, Func<Envelope, UniTask>>> _map = new();

        public ClientMessageRouter(ISerializer ser, MessageTypeRegistry messageTypeRegistry)
        {
            _ser = ser;
            _messageTypeRegistry = messageTypeRegistry;
        }

        public void Register<T>(IClientMessageHandler<T> handler) where T : struct, IMessagePayload
        {
            var type = _messageTypeRegistry.GetId<T>();
            if(!_map.ContainsKey(type))
                _map.Add(type, new Dictionary<object, Func<Envelope, UniTask>>());
            
            _map[type].Add(handler, async (env) =>
            {
                var dto = _ser.Deserialize<T>(env.Payload);
                var ctx = new MessageContext
                {
                    ServerTick = env.ServerTick,
                    ClientTick = env.ClientTick,
                    Source = env.Source,
                    CorrelationId = env.Id
                };
                await handler.HandleAsync(dto, ctx);
            });
        }

        public void Unregister<T>(IClientMessageHandler<T> handler) where T : struct, IMessagePayload
        {
            var type = _messageTypeRegistry.GetId<T>();
            if(!_map.TryGetValue(type, out var handlers))
                return;
            
            handlers.Remove(handler);
        }

        public UniTask RouteAsync(Envelope envelope)
        {
            if(!_map.TryGetValue(envelope.Type, out var handlers))
                return UniTask.CompletedTask;
            return UniTask.WhenAll(handlers.Values.Select(f => f(envelope)));
        }
    }
}