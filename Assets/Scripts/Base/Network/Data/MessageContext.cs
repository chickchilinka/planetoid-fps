namespace Base.Network.Data
{
    public struct MessageContext
    {
        public uint ServerTick;
        public uint ClientTick;
        public ConnectionId Source;
        public MessageId CorrelationId;
    }
}