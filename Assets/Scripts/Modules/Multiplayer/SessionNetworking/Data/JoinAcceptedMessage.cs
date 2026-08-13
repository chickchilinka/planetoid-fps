using Base.Network.Data;
using MessagePack;

namespace Modules.Multiplayer.Session.Networking
{
    [MessagePackObject]
    public struct JoinAcceptedMessage : IMessagePayload
    {
        [Key(0)]
        public string PlayerId { get; set; }

        [Key(1)]
        public string SessionId { get; set; }

        [Key(2)]
        public long Revision { get; set; }
    }
}
