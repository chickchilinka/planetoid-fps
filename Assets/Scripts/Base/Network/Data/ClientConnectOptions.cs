using System;

namespace Base.Network.Data
{
    public struct ClientConnectOptions
    {
        public string Host;
        public ushort Port;
        public TimeSpan Timeout;
    }
}