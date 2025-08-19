using System;

namespace Base.Network.Data
{
    public class MessageTypeRegistration
    {
        public Type MessageType;
        public ushort TypeId;
        public Reliability DefaultReliability;
    }
}