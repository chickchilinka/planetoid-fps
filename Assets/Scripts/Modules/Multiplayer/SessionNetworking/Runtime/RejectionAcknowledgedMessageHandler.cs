using System;
using Base.Network.Data;
using Base.Network.Handler;
using Cysharp.Threading.Tasks;

namespace Modules.Multiplayer.Session.Networking
{
    public sealed class RejectionAcknowledgedMessageHandler : IConnectionMessageHandler<RejectionAcknowledgedMessage>
    {
        private readonly ServerSessionNetworkBridge _bridge;

        public RejectionAcknowledgedMessageHandler(ServerSessionNetworkBridge bridge)
        {
            _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
        }

        public UniTask HandleAsync(RejectionAcknowledgedMessage message, ConnectionId from, MessageContext context)
        {
            return _bridge.HandleRejectionAcknowledgedAsync(from, context);
        }
    }
}
