namespace Modules.Multiplayer.Spawning
{
    public interface ISpawnTelemetry
    {
        void Record(SpawnTelemetryEvent value);
    }
}
