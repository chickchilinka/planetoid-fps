using System;
using FishNet.Broadcast;

namespace Features.FishNetworking.Impl.Data
{
    public struct RawEnvelope : IBroadcast
    {
        public uint Id;
        public uint ReplyTo; 
        public ushort Type;
        public uint ServerTick;
        public uint ClientTick;
        public ArraySegment<byte> Payload;
    }
}