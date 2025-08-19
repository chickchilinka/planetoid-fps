using System;
using Base.Network.Data;
using FishNet.Broadcast;
using FishNet.Transporting;
using MessagePack;

namespace Features.FishNetworking.Impl.Data
{
    public struct RawEnvelope : IBroadcast
    {
        public uint Id;
        public ushort Type;
        public uint ServerTick;
        public uint ClientTick;
        public ArraySegment<byte> Payload;
    }
}