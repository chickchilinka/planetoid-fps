using Base.Network.Data;
using MessagePack;

namespace Modules.Multiplayer.Session.Networking
{
    [MessagePackObject]
    public struct SetReadyMessage : IMessagePayload
    {
        [Key(0)]
        public bool Ready { get; set; }
    }
}
