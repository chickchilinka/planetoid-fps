using System.Threading;
using System.Threading.Tasks;
using Modules.Multiplayer.Primitives;

namespace Modules.Multiplayer.Spawning
{
    public interface IPlayerEntityRuntimeProvider
    {
        ValueTask<Result<RuntimeEntityHandle, SpawnError>> CreateAsync(PlayerEntityCreateRequest request, CancellationToken token);
        ValueTask<Result<Unit, SpawnError>> DestroyAsync(RuntimeEntityHandle handle, CancellationToken token);
    }
}
