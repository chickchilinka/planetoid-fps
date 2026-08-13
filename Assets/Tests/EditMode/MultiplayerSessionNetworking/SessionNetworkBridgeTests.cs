using Base.Network.Data;
using Modules.Multiplayer.Primitives;
using NUnit.Framework;

namespace Modules.Multiplayer.Session.Networking.Tests
{
    public sealed class SessionNetworkBridgeTests
    {
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
    }
}
