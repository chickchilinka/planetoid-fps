using Base.Network.Data;
using Base.Network.Factory;
using Base.Network.Model;
using Base.Network.Provider;
using Base.Network.Service;
using Base.Network.Storage;
using Cysharp.Threading.Tasks;
using Modules.Multiplayer.Primitives;
using Modules.Multiplayer.Session;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UniRx;
using SessionUnit = Modules.Multiplayer.Primitives.Unit;

namespace Modules.Multiplayer.Session.Networking.Tests
{
    public sealed class SessionNetworkBridgeTests
    {
        [Test]
        public async Task RejectedJoin_SendsRejectionWithoutImmediateDisconnect_AndAcknowledgementCloses()
        {
            var transport = new RecordingServer();
            var connection = new RecordingConnection(new ConnectionId(42));
            var delay = new ControlledRejectionDelay();
            var bridge = CreateRejectingBridge(transport, delay);

            await bridge.HandleConnectedAsync(connection);

            Assert.That(connection.DisconnectCount, Is.EqualTo(0));
            Assert.That(transport.Sent, Has.Count.EqualTo(1));
            Assert.That(transport.Sent[0].Type, Is.EqualTo(SessionMessageTypeIds.CommandRejected));
            await bridge.HandleRejectionAcknowledgedAsync(connection.Id, new MessageContext { Source = connection.Id });
            Assert.That(connection.DisconnectCount, Is.EqualTo(1));
            bridge.Dispose();
        }

        [Test]
        public async Task RejectedJoin_ClosesAfterBoundedGraceWhenAcknowledgementIsMissing()
        {
            var transport = new RecordingServer();
            var connection = new RecordingConnection(new ConnectionId(42));
            var delay = new ControlledRejectionDelay();
            var bridge = CreateRejectingBridge(transport, delay);
            await bridge.HandleConnectedAsync(connection);

            delay.Release();
            await Task.Delay(10);

            Assert.That(connection.DisconnectCount, Is.EqualTo(1));
            bridge.Dispose();
        }

        [Test]
        public void Registry_AttachesPlayerAndResolvesBothDirections()
        {
            var registry = new SessionConnectionRegistry();
            var transport = new ConnectionId(42);
            var player = SessionNetworkingFixtures.Player("40000000-0000-0000-0000-000000000004");
            var session = registry.Register(transport);

            registry.AttachPlayer(session, player);

            Assert.That(registry.TryGetPlayer(transport, out var resolvedPlayer), Is.True);
            Assert.That(resolvedPlayer, Is.EqualTo(player));
            Assert.That(registry.TryGetConnection(player, out var resolvedTransport), Is.True);
            Assert.That(resolvedTransport.Value, Is.EqualTo(transport.Value));
        }

        [Test]
        public void Unregister_RemovesConnectionAndPlayerMappingsAtomically()
        {
            var registry = new SessionConnectionRegistry();
            var transport = new ConnectionId(42);
            var player = SessionNetworkingFixtures.Player("40000000-0000-0000-0000-000000000004");
            registry.AttachPlayer(registry.Register(transport), player);

            var removed = registry.Unregister(transport, out var removedPlayer);

            Assert.That(removed, Is.True);
            Assert.That(removedPlayer, Is.EqualTo(player));
            Assert.That(registry.TryGetSession(transport, out _), Is.False);
            Assert.That(registry.TryGetPlayer(transport, out _), Is.False);
            Assert.That(registry.TryGetConnection(player, out _), Is.False);
            Assert.That(registry.AttachedConnections(), Is.Empty);
        }

        [Test]
        public void Registry_DoesNotExposeUnattachedConnectionToSnapshotPublisher()
        {
            var registry = new SessionConnectionRegistry();
            registry.Register(new ConnectionId(42));

            Assert.That(registry.AttachedConnections(), Is.Empty);
        }

        [Test]
        public void Registry_RejectsDuplicateTransportConnection()
        {
            var registry = new SessionConnectionRegistry();
            registry.Register(new ConnectionId(42));

            Assert.Throws<System.InvalidOperationException>(() => registry.Register(new ConnectionId(42)));
        }

        private static ServerSessionNetworkBridge CreateRejectingBridge(
            RecordingServer server, IRejectionCloseDelay delay)
        {
            var types = new MessageTypeRegistry();
            types.Add(new MessageTypeRegistration
            {
                MessageType = typeof(SessionCommandRejectedMessage),
                TypeId = SessionMessageTypeIds.CommandRejected,
                DefaultReliability = Reliability.Reliable
            });
            var messenger = new ServerMessenger(server,
                new EnvelopeFactory(new FixedTicks(), new MessageIdGenerator(), new EmptySerializer(), types),
                new EmptySerializer());
            return new ServerSessionNetworkBridge(server, new RejectingSession(), messenger,
                new SessionConnectionRegistry(), delay);
        }

        private sealed class ControlledRejectionDelay : IRejectionCloseDelay
        {
            private readonly UniTaskCompletionSource _completion = new UniTaskCompletionSource();
            public UniTask WaitAsync(CancellationToken token) => _completion.Task.AttachExternalCancellation(token);
            public void Release() => _completion.TrySetResult();
        }

        private sealed class RejectingSession : IServerSessionFacade
        {
            public SessionSnapshot Snapshot => SessionNetworkingFixtures.PlayingWithThreePlayers();
            public ValueTask<Result<PlayerId, SessionError>> JoinAsync(SessionConnection connection, CancellationToken token)
                => new ValueTask<Result<PlayerId, SessionError>>(Result<PlayerId, SessionError>.Failure(SessionError.SessionFull));
            public ValueTask<Result<SessionUnit, SessionError>> LeaveAsync(PlayerId playerId, CancellationToken token) => throw new NotSupportedException();
            public ValueTask<Result<SessionUnit, SessionError>> SetReadyAsync(PlayerId playerId, bool ready, CancellationToken token) => throw new NotSupportedException();
            public ValueTask<Result<SessionUnit, SessionError>> NotifyServerWorldReadyAsync(OperationId operationId, MatchId matchId, CancellationToken token) => throw new NotSupportedException();
            public ValueTask<Result<SessionUnit, SessionError>> NotifyPlayerWorldReadyAsync(OperationId operationId, PlayerId playerId, MatchId matchId, CancellationToken token) => throw new NotSupportedException();
        }

        private sealed class RecordingServer : INetworkServer
        {
            public List<Envelope> Sent { get; } = new List<Envelope>();
            public IObservable<IConnection> OnConnected => Observable.Never<IConnection>();
            public IObservable<(ConnectionId, DisconnectReason)> OnDisconnected => Observable.Never<(ConnectionId, DisconnectReason)>();
            public UniTask StartAsync(ServerStartOptions opts) => UniTask.CompletedTask;
            public UniTask StopAsync() => UniTask.CompletedTask;
            public UniTask BroadcastAsync(in Envelope envelope, ReadOnlySpan<ConnectionId> except = default) { Sent.Add(envelope); return UniTask.CompletedTask; }
            public UniTask BroadcastAsync(ConnectionId to, in Envelope envelope) { Sent.Add(envelope); return UniTask.CompletedTask; }
        }

        private sealed class RecordingConnection : IConnection
        {
            public RecordingConnection(ConnectionId id) => Id = id;
            public ConnectionId Id { get; }
            public int DisconnectCount { get; private set; }
            public IObservable<Envelope> OnMessage => Observable.Never<Envelope>();
            public UniTask SendAsync(in Envelope envelope) => UniTask.CompletedTask;
            public UniTask DisconnectAsync(DisconnectReason reason) { DisconnectCount++; return UniTask.CompletedTask; }
        }

        private sealed class FixedTicks : ITickSource
        {
            public uint ServerTick => 0;
            public uint ClientTick => 0;
        }

        private sealed class EmptySerializer : ISerializer
        {
            public ArraySegment<byte> Serialize<T>(in T value) where T : struct, IMessagePayload => default;
            public T Deserialize<T>(ArraySegment<byte> bytes) where T : struct, IMessagePayload => default;
        }
    }
}
