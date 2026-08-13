using System.Threading;
using System.Threading.Tasks;
using Modules.Multiplayer.Primitives;

namespace Modules.Multiplayer.Spawning
{
    public interface IPlayerSpawnService
    {
        ValueTask<Result<SpawnHandle, SpawnError>> SpawnAsync(PlayerSpawnRequest request, CancellationToken token);
        ValueTask<Result<DespawnOutcome, SpawnError>> DespawnAsync(PlayerId playerId, CancellationToken token);
    }
}
