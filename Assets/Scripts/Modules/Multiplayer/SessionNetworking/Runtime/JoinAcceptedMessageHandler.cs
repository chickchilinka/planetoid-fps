using System;
using Base.Network.Data;
using Base.Network.Handler;
using Cysharp.Threading.Tasks;

namespace Modules.Multiplayer.Session.Networking
{
    public sealed class JoinAcceptedMessageHandler : IClientMessageHandler<JoinAcceptedMessage>
    {
        private readonly ClientSessionNetworkBridge _bridge;

        public JoinAcceptedMessageHandler(ClientSessionNetworkBridge bridge)
        {
            _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
        }

        public UniTask HandleAsync(in JoinAcceptedMessage message, in MessageContext context)
        {
            _bridge.ApplyJoinAccepted(message);
            return UniTask.CompletedTask;
        }
    }
}
