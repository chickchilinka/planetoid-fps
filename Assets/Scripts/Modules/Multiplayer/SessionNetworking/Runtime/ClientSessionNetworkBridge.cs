using System;
using System.Threading;
using System.Threading.Tasks;
using Base.Network.Service;
using Cysharp.Threading.Tasks;
using Modules.Multiplayer.Primitives;
using Modules.Multiplayer.Session;

namespace Modules.Multiplayer.Session.Networking
{
    public sealed class ClientSessionNetworkBridge : IClientSessionCommandPublisher
    {
        private readonly ClientMessenger _messenger;

        public ClientSessionNetworkBridge(ClientMessenger messenger)
        {
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        }

        public PlayerId LocalPlayerId { get; private set; }
        public SessionId SessionId { get; private set; }
        public long AcceptedRevision { get; private set; } = -1;
        public SessionError? LastRejection { get; private set; }
        public event Action<SessionError> CommandRejected;

        public async ValueTask<Result<Unit, SessionError>> SetReadyAsync(bool ready, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var message = new SetReadyMessage { Ready = ready };
            await _messenger.Send(message);
            return Result<Unit, SessionError>.Success(Unit.Value);
        }

        public void ApplyJoinAccepted(JoinAcceptedMessage message)
        {
            LocalPlayerId = SessionDtoMapper.ToPlayerId(message);
            SessionId = SessionDtoMapper.ToSessionId(message);
            AcceptedRevision = SessionDtoMapper.ToRevision(message);
            LastRejection = null;
        }

        public void ApplyRejection(SessionCommandRejectedMessage message)
        {
            var error = SessionDtoMapper.ToDomain(message);
            LastRejection = error;
            CommandRejected?.Invoke(error);
        }
    }
}
