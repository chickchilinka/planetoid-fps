using System;
using System.Threading;
using System.Threading.Tasks;
using Modules.Multiplayer.Primitives;

namespace Modules.Multiplayer.Session
{
    public interface IClientSessionFacade
    {
        SessionSnapshot Snapshot { get; }
        event Action<SessionSnapshot> SnapshotChanged;
        void ApplySnapshot(SessionSnapshot snapshot);
        ValueTask<Result<Unit, SessionError>> SetReadyAsync(bool ready, CancellationToken token);
    }
}
