namespace Modules.Multiplayer.Session
{
    public enum SessionError
    {
        SessionFull,
        UnknownPlayer,
        InvalidPhase,
        NotEnoughPlayers,
        WorldLoadFailed,
        WorldLoadTimeout,
        PlayerLoadTimeout,
        SpawnFailed,
        ConnectionClosed
    }
}
