using System;
using System.Collections.Generic;
using Base.Network.Data;
using Base.Network.Handler;
using Base.Network.Model;
using Base.Network.Provider;
using Base.Network.Storage;
using Cysharp.Threading.Tasks;

namespace Base.Network.Routing
{
    public sealed class ConnectionMessageRouter
    {
        private readonly ISerializer _ser;
        private readonly MessageTypeRegistry _messageTypeRegistry;
        private readonly Dictionary<ushort, Dictionary<object, Func<Envelope, IConnection, UniTask>>> _map = new();

        public ConnectionMessageRouter(ISerializer ser, MessageTypeRegistry messageTypeRegistry)
        {
            _ser = ser;
            _messageTypeRegistry = messageTypeRegistry;
        }

        public void Register<T>(IConnectionMessageHandler<T> handler) where T : struct, IMessagePayload
        {
            var type = _messageTypeRegistry.GetId<T>();
            if(!_map.ContainsKey(type))
                _map.Add(type, new Dictionary<object, Func<Envelope, IConnection, UniTask>>());
            
            _map[type].Add(handler, async (env, from) =>
            {
                var dto = _ser.Deserialize<T>(env.Payload);
                var ctx = new MessageContext
                {
                    ServerTick = env.ServerTick,
                    ClientTick = env.ClientTick,
                    Source = env.Source,
                    CorrelationId = env.Id
                };
                await handler.HandleAsync(dto, from.Id, ctx);
            });
        }

        public void Unregister<T>(IConnectionMessageHandler<T> handler) where T : struct, IMessagePayload
        {
            var type = _messageTypeRegistry.GetId<T>();
            if(!_map.TryGetValue(type, out var handlers))
                return;
            
            handlers.Remove(handler);
        }
        
        public UniTask RouteAsync(Envelope envelope, IConnection from)
        {
            if(!_map.TryGetValue(envelope.Type, out var handlers))
                return UniTask.CompletedTask;
            
            return UniTask.WhenAll(handlers.Values.Select(f => f(envelope, from)));
        }
    }
}