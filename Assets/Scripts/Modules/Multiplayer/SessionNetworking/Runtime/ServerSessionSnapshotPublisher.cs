using System;
using System.Threading;
using System.Threading.Tasks;
using Base.Network.Service;
using Modules.Multiplayer.Session;

namespace Modules.Multiplayer.Session.Networking
{
    /// <summary>Publishes authoritative snapshots only to connections whose joins completed.</summary>
    public sealed class ServerSessionSnapshotPublisher : ISessionEventPublisher
    {
        private readonly ServerMessenger _messenger;
        private readonly SessionConnectionRegistry _registry;

        public ServerSessionSnapshotPublisher(ServerMessenger messenger, SessionConnectionRegistry registry)
        {
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public async ValueTask PublishSnapshotAsync(SessionSnapshot snapshot, CancellationToken token)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            token.ThrowIfCancellationRequested();
            var message = SessionDtoMapper.ToDto(snapshot);
            foreach (var connection in _registry.AttachedConnections())
                await _messenger.To(connection, message);
        }
    }
}
