using System;
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
        private readonly CancellationTokenSource _lifetime = new();
        private IDisposable _connectedSubscription;
        private IDisposable _disconnectedSubscription;

        public ServerSessionNetworkBridge(INetworkServer server, IServerSessionFacade session, ServerMessenger messenger,
            SessionConnectionRegistry registry)
        {
            _server = server ?? throw new ArgumentNullException(nameof(server));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
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
            // Unregister first so a reentrant snapshot publication never observes a disconnected recipient.
            if (_registry.Unregister(connectionId, out var playerId) && playerId.IsValid)
                await _session.LeaveAsync(playerId, _lifetime.Token);
        }

        public void Dispose()
        {
            _connectedSubscription?.Dispose();
            _disconnectedSubscription?.Dispose();
            _lifetime.Cancel();
            _lifetime.Dispose();
        }

        private async UniTask RejectAndDisconnectAsync(IConnection connection, SessionError error)
        {
            // Send reliably before closing; the transport may still drop it, but order is explicit.
            await _messenger.To(connection.Id, SessionDtoMapper.ToDto(error));
            await connection.DisconnectAsync(DisconnectReason.ClosedByServer);
        }
    }
}
