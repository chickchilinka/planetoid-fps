using System.Threading;
using Cysharp.Threading.Tasks;

namespace Modules.Multiplayer.Session.Networking
{
    public interface IRejectionCloseDelay
    {
        UniTask WaitAsync(CancellationToken token);
    }

    public sealed class RejectionCloseDelay : IRejectionCloseDelay
    {
        public UniTask WaitAsync(CancellationToken token) => UniTask.Delay(3000, cancellationToken: token);
    }
}
