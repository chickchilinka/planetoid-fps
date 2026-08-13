using System.Threading;
using System.Threading.Tasks;
using Modules.Multiplayer.Primitives;

namespace Modules.Multiplayer.Session
{
    public interface IMatchWorldProvider
    {
        ValueTask<Result<Unit, SessionError>> LoadAsync(MatchWorldLoadRequest request, CancellationToken token);
        ValueTask CancelAsync(OperationId operationId, CancellationToken token);
    }
}
