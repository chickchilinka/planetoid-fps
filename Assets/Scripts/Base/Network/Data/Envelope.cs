using System;

namespace Base.Network.Data
{
    public struct Envelope
    {
        public MessageId Id;
        public ushort Type;
        public uint ServerTick;
        public uint ClientTick;
        public ConnectionId Source;
        public Reliability Reliability;
        public ArraySegment<byte> Payload;
    }
}