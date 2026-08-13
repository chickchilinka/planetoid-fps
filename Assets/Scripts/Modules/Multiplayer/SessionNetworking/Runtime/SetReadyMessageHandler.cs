using System;
using Base.Network.Data;
using Base.Network.Handler;
using Base.Network.Service;
using Cysharp.Threading.Tasks;
using Modules.Multiplayer.Session;

namespace Modules.Multiplayer.Session.Networking
{
    public sealed class SetReadyMessageHandler : IConnectionMessageHandler<SetReadyMessage>
    {
        private readonly SessionConnectionRegistry _registry;
        private readonly IServerSessionFacade _session;
        private readonly ServerMessenger _messenger;

        public SetReadyMessageHandler(SessionConnectionRegistry registry, IServerSessionFacade session, ServerMessenger messenger)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        }

        public async UniTask HandleAsync(SetReadyMessage message, ConnectionId from, MessageContext context)
        {
            if (context.Source.Value != from.Value || !_registry.TryGetPlayer(from, out var playerId))
            {
                await _messenger.To(from, SessionDtoMapper.ToDto(SessionError.UnknownPlayer));
                return;
            }

            var result = await _session.SetReadyAsync(playerId, message.Ready, default);
            if (!result.IsSuccess)
                await _messenger.To(from, SessionDtoMapper.ToDto(result.Error));
        }
    }
}
