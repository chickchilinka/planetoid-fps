using System.Threading;
using System.Threading.Tasks;
using Modules.Multiplayer.Primitives;

namespace Modules.Multiplayer.Session
{
    public interface IClientSessionCommandPublisher
    {
        ValueTask<Result<Unit, SessionError>> SetReadyAsync(bool ready, CancellationToken token);
    }
}
