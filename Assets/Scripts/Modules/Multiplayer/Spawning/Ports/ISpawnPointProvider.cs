using System.Collections.Generic;
using Modules.Multiplayer.Primitives;

namespace Modules.Multiplayer.Spawning
{
    public interface ISpawnPointProvider
    {
        Result<SpawnReservation, SpawnError> Reserve(PlayerId playerId, IReadOnlyCollection<string> excludedPointIds);
        void Release(SpawnReservation reservation);
    }
}
