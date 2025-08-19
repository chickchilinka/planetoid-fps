using System;
using System.Buffers;
using System.Threading;
using Base.Network.Data;
using Base.Network.Provider;
using MessagePack;

namespace Features.FishNetworking.Impl.Utils
{
    public sealed class MessagePackNetworkSerializer : ISerializer
    {
        private readonly MessagePackSerializerOptions _options;
        public MessagePackNetworkSerializer(MessagePackSerializerOptions opts = null)
            => _options = opts ?? MessagePackSerializer.DefaultOptions;
        
        public ArraySegment<byte> Serialize<T>(in T value) where T : struct, IMessagePayload
            => MessagePackSerializer.Serialize(value, _options);

        public T Deserialize<T>(ArraySegment<byte> bytes) where T : struct, IMessagePayload
            => MessagePackSerializer.Deserialize<T>(bytes, _options);
    }
}