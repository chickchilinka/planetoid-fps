using System;
using Base.Network.Data;
using Features.FishNetworking.Impl.Data;
using FishNet.Connection;
using FishNet.Transporting;

namespace Features.FishNetworking.Impl.Utils
{
    public static class FishNetEnvelope
    {
        public static RawEnvelope ToRaw(in Envelope env)
            => new RawEnvelope
            {
                Id = env.Id.Value,
                Type = env.Type,
                ServerTick = env.ServerTick,
                ClientTick = env.ClientTick,
                Payload = env.Payload
            };

        public static Envelope FromRaw(in RawEnvelope raw, Channel channel, NetworkConnection conn = null)
            => new()
            {
                Id = new MessageId(raw.Id),
                Type = raw.Type,
                ServerTick = raw.ServerTick,
                ClientTick = raw.ClientTick,
                Payload = raw.Payload,
                Reliability = ReliabilityFromChannel(channel),
                Source = conn != null ? new ConnectionId(conn.ClientId) : default,
            };

        public static Channel ChannelFromReliability(Reliability reliability) => reliability switch
        {
            Reliability.Reliable => Channel.Reliable,
            Reliability.Unreliable => Channel.Unreliable,
            _ => throw new ArgumentOutOfRangeException(nameof(reliability), reliability, null)
        };

        public static Reliability ReliabilityFromChannel(Channel channel) => channel switch
        {
            Channel.Reliable => Reliability.Reliable,
            Channel.Unreliable => Reliability.Unreliable,
            _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, null)
        };
    }
}