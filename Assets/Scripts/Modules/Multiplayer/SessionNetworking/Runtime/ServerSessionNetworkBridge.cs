using System;
using System.Collections.Generic;
using System.Threading;
using Base.Network.Data;
using Base.Network.Model;
using Base.Network.Provider;
using Base.Network.Service;
using Cysharp.Threading.Tasks;
using Modules.Multiplayer.Primitives;
using Modules.Multiplayer.Session;
using UniRx;
using Zenject;

namespace Modules.Multiplayer.Session.Networking
{
    public sealed class ServerSessionNetworkBridge : IInitializable, IDisposable
    {
        private readonly INetworkServer _server;
        private readonly IServerSessionFacade _session;
        private readonly ServerMessenger _messenger;
        private readonly SessionConnectionRegistry _registry;
        private readonly IRejectionCloseDelay _rejectionCloseDelay;
        private readonly CancellationTokenSource _lifetime = new();
        private readonly Dictionary<int, PendingRejection> _pendingRejections = new();
        private IDisposable _connectedSubscription;
        private IDisposable _disconnectedSubscription;

        public ServerSessionNetworkBridge(INetworkServer server, IServerSessionFacade session, ServerMessenger messenger,
            SessionConnectionRegistry registry, IRejectionCloseDelay rejectionCloseDelay)
        {
            _server = server ?? throw new ArgumentNullException(nameof(server));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _rejectionCloseDelay = rejectionCloseDelay ?? throw new ArgumentNullException(nameof(rejectionCloseDelay));
        }

        public void Initialize()
        {
            _connectedSubscription = _server.OnConnected.Subscribe(connection => HandleConnectedAsync(connection).Forget());
            _disconnectedSubscription = _server.OnDisconnected.Subscribe(value =>
                HandleDisconnectedAsync(value.Item1, value.Item2).Forget());
        }

        public async UniTask HandleConnectedAsync(IConnection connection)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            var sessionConnection = _registry.Register(connection.Id);
            try
            {
                var joined = await _session.JoinAsync(sessionConnection, _lifetime.Token);
                if (!joined.IsSuccess)
                {
                    _registry.Unregister(connection.Id, out _);
                    await RejectAndDisconnectAsync(connection, joined.Error);
                    return;
                }

                // A disconnect can happen while JoinAsync awaits. Do not resurrect that player.
                if (!_registry.TryGetSession(connection.Id, out var registered) || registered != sessionConnection)
                {
                    await _session.LeaveAsync(joined.Value, _lifetime.Token);
                    return;
                }

                _registry.AttachPlayer(sessionConnection, joined.Value);
                await _messenger.To(connection.Id, SessionDtoMapper.ToJoinAccepted(joined.Value, _session.Snapshot));
                await _messenger.To(connection.Id, SessionDtoMapper.ToDto(_session.Snapshot));
            }
            catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
            {
            }
            catch
            {
                if (_registry.Unregister(connection.Id, out var playerId) && playerId.IsValid)
                    await _session.LeaveAsync(playerId, _lifetime.Token);
                await connection.DisconnectAsync(DisconnectReason.ClosedByServer);
                throw;
            }
        }

        public async UniTask HandleDisconnectedAsync(ConnectionId connectionId, DisconnectReason reason)
        {
            if (_pendingRejections.Remove(connectionId.Value, out var pending))
                pending.Cancel();
            // Unregister first so a reentrant snapshot publication never observes a disconnected recipient.
            if (_registry.Unregister(connectionId, out var playerId) && playerId.IsValid)
                await _session.LeaveAsync(playerId, _lifetime.Token);
        }

        public UniTask HandleRejectionAcknowledgedAsync(ConnectionId from, MessageContext context)
        {
            if (context.Source.Value != from.Value || !_pendingRejections.Remove(from.Value, out var pending))
                return UniTask.CompletedTask;

            pending.Cancel();
            return pending.Connection.DisconnectAsync(DisconnectReason.ClosedByServer);
        }

        public void Dispose()
        {
            _connectedSubscription?.Dispose();
            _disconnectedSubscription?.Dispose();
            _lifetime.Cancel();
            foreach (var pending in _pendingRejections.Values) pending.Cancel();
            _pendingRejections.Clear();
            _lifetime.Dispose();
        }

        private async UniTask RejectAndDisconnectAsync(IConnection connection, SessionError error)
        {
            // Base.Network has no transport flush or delivery acknowledgement. Keep the connection alive
            // until the client confirms receipt, while a short bounded timeout guarantees cleanup.
            await _messenger.To(connection.Id, SessionDtoMapper.ToDto(error));
            var pending = new PendingRejection(connection);
            _pendingRejections.Add(connection.Id.Value, pending);
            CloseAfterRejectionGraceAsync(connection.Id, pending).Forget();
        }

        private async UniTask CloseAfterRejectionGraceAsync(ConnectionId connectionId, PendingRejection pending)
        {
            try
            {
                await _rejectionCloseDelay.WaitAsync(pending.Token);
                if (_pendingRejections.Remove(connectionId.Value, out var current) && ReferenceEquals(current, pending))
                    await pending.Connection.DisconnectAsync(DisconnectReason.ClosedByServer);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                pending.Dispose();
            }
        }

        private sealed class PendingRejection : IDisposable
        {
            private readonly CancellationTokenSource _cancellation = new();

            public PendingRejection(IConnection connection) => Connection = connection;
            public IConnection Connection { get; }
            public CancellationToken Token => _cancellation.Token;
            public void Cancel() => _cancellation.Cancel();
            public void Dispose() => _cancellation.Dispose();
        }
    }
}
