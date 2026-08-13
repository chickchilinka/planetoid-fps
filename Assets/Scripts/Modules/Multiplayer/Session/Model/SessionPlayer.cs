using Modules.Multiplayer.Primitives;

namespace Modules.Multiplayer.Session
{
    internal sealed class SessionPlayer
    {
        public SessionPlayer(PlayerId playerId, SessionConnection connection, SessionJoinKind joinKind)
        {
            PlayerId = playerId;
            Connection = connection;
            JoinKind = joinKind;
        }

        public PlayerId PlayerId { get; }
        public SessionConnection Connection { get; }
        public SessionJoinKind JoinKind { get; }
        public bool Ready { get; set; }
        public bool WorldReady { get; set; }
        public SpawnState SpawnState { get; set; }

        public SessionPlayerSnapshot ToSnapshot()
        {
            return new SessionPlayerSnapshot(PlayerId, Ready, WorldReady, SpawnState, JoinKind);
        }
    }
}
