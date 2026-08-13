using System.Threading;
using System.Threading.Tasks;

namespace Modules.Multiplayer.Session
{
    public interface ISessionConnectionProvider
    {
        ValueTask DisconnectAsync(
            SessionConnection connection,
            SessionError reason,
            CancellationToken token);
    }
}
