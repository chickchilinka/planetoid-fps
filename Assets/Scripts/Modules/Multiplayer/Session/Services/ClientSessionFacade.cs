using System;
using System.Threading;
using System.Threading.Tasks;
using Modules.Multiplayer.Primitives;

namespace Modules.Multiplayer.Session
{
    public sealed class ClientSessionFacade : IClientSessionFacade
    {
        private readonly IClientSessionCommandPublisher _commandPublisher;
        private SessionSnapshot _snapshot;

        public ClientSessionFacade(IClientSessionCommandPublisher commandPublisher)
        {
            _commandPublisher = commandPublisher ?? throw new ArgumentNullException(nameof(commandPublisher));
        }

        public SessionSnapshot Snapshot => _snapshot;

        public event Action<SessionSnapshot> SnapshotChanged;

        public void ApplySnapshot(SessionSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (_snapshot != null && snapshot.SessionId == _snapshot.SessionId && snapshot.Revision <= _snapshot.Revision)
                return;

            _snapshot = snapshot;
            SnapshotChanged?.Invoke(snapshot);
        }

        public ValueTask<Result<Unit, SessionError>> SetReadyAsync(bool ready, CancellationToken token)
        {
            return _commandPublisher.SetReadyAsync(ready, token);
        }
    }
}
