using System.Threading;
using System.Threading.Tasks;

namespace Modules.Multiplayer.Session
{
    public interface ISessionEventPublisher
    {
        ValueTask PublishSnapshotAsync(SessionSnapshot snapshot, CancellationToken token);
    }
}
