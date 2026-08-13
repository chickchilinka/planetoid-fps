namespace Modules.Multiplayer.Spawning
{
    public enum SpawnError
    {
        ConnectionUnavailable,
        WorldNotReady,
        NoSpawnPoint,
        EntityCreationFailed,
        Cancelled
    }

    public enum DespawnOutcome
    {
        Despawned,
        AlreadyAbsent
    }
}
