using System;
using Base.Network.Data;
using Base.Network.Handler;
using Cysharp.Threading.Tasks;
using Modules.Multiplayer.Session;

namespace Modules.Multiplayer.Session.Networking
{
    public sealed class SessionSnapshotMessageHandler : IClientMessageHandler<SessionSnapshotMessage>
    {
        private readonly IClientSessionFacade _session;

        public SessionSnapshotMessageHandler(IClientSessionFacade session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public UniTask HandleAsync(in SessionSnapshotMessage message, in MessageContext context)
        {
            _session.ApplySnapshot(SessionDtoMapper.ToDomain(message));
            return UniTask.CompletedTask;
        }
    }
}
