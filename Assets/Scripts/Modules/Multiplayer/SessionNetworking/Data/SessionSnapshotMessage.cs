using Base.Network.Data;
using MessagePack;

namespace Modules.Multiplayer.Session.Networking
{
    [MessagePackObject]
    public struct SessionSnapshotMessage : IMessagePayload
    {
        [Key(0)]
        public string SessionId { get; set; }

        [Key(1)]
        public string MatchId { get; set; }

        [Key(2)]
        public string MapId { get; set; }

        [Key(3)]
        public int Phase { get; set; }

        [Key(4)]
        public long Revision { get; set; }

        [Key(5)]
        public bool ServerWorldReady { get; set; }

        [Key(6)]
        public SessionPlayerSnapshotMessage[] Players { get; set; }
    }

    [MessagePackObject]
    public struct SessionPlayerSnapshotMessage
    {
        [Key(0)]
        public string PlayerId { get; set; }

        [Key(1)]
        public bool Ready { get; set; }

        [Key(2)]
        public bool WorldReady { get; set; }

        [Key(3)]
        public int SpawnState { get; set; }

        [Key(4)]
        public int JoinKind { get; set; }
    }
}
