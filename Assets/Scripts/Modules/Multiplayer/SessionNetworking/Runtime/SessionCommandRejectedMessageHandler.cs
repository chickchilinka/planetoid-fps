using System;
using Base.Network.Data;
using Base.Network.Handler;
using Cysharp.Threading.Tasks;

namespace Modules.Multiplayer.Session.Networking
{
    public sealed class SessionCommandRejectedMessageHandler : IClientMessageHandler<SessionCommandRejectedMessage>
    {
        private readonly ClientSessionNetworkBridge _bridge;

        public SessionCommandRejectedMessageHandler(ClientSessionNetworkBridge bridge)
        {
            _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
        }

        public UniTask HandleAsync(in SessionCommandRejectedMessage message, in MessageContext context)
        {
            return _bridge.ApplyRejectionAsync(message);
        }
    }
}
