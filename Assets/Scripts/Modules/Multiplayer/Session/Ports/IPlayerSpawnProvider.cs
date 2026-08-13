using System.Threading;
using System.Threading.Tasks;
using Modules.Multiplayer.Primitives;

namespace Modules.Multiplayer.Session
{
    public interface IPlayerSpawnProvider
    {
        ValueTask<Result<SpawnPlayerResult, SessionError>> SpawnAsync(
            SpawnPlayerRequest request,
            CancellationToken token);
        ValueTask<Result<Unit, SessionError>> DespawnAsync(PlayerId playerId, CancellationToken token);
    }
}
