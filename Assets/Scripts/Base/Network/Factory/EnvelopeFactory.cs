using Base.Network.Data;
using Base.Network.Provider;
using Base.Network.Storage;

namespace Base.Network.Factory
{
    public class EnvelopeFactory
    {
        private readonly ITickSource _ticks;
        private readonly MessageIdGenerator _ids;
        private readonly ISerializer _ser;
        private readonly MessageTypeRegistry _types;

        public EnvelopeFactory(ITickSource t, MessageIdGenerator ids, ISerializer s, MessageTypeRegistry types)
        {
            _ticks = t;
            _ids = ids;
            _ser = s;
            _types = types;
        }

        public Envelope Create<T>(in T payload, ConnectionId source, Reliability? qos = null)
            where T : struct, IMessagePayload
        {
            var id = _types.GetId<T>();
            var rel = qos ?? _types.GetDefaultReliability<T>();
            return new Envelope
            {
                Id = _ids.Next(),
                ReplyTo = default,
                Type = id,
                ServerTick = _ticks.ServerTick,
                ClientTick = _ticks.ClientTick,
                Source = source,
                Payload = _ser.Serialize(payload),
                Reliability = rel
            };
        }
        
        public Envelope CreateReply<T>(in T payload, ConnectionId src, MessageId replyTo, Reliability? qos=null)
            where T: struct, IMessagePayload
        {
            var e = Create(payload, src, qos);
            e.ReplyTo = replyTo;
            return e;
        }
    }
}