using Base.Network.Data;
using MessagePack;

namespace Modules.Multiplayer.Session.Networking
{
    [MessagePackObject]
    public struct SessionCommandRejectedMessage : IMessagePayload
    {
        [Key(0)]
        public int Error { get; set; }
    }
}
