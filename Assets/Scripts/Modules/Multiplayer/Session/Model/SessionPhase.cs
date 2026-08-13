namespace Modules.Multiplayer.Session
{
    public enum SessionPhase
    {
        WaitingForPlayers,
        Lobby,
        LoadingMatch,
        Playing,
        Stopping
    }

    public enum SessionJoinKind
    {
        Initial,
        InProgress
    }

    public enum SpawnState
    {
        NotSpawned,
        Spawning,
        Spawned
    }
}
