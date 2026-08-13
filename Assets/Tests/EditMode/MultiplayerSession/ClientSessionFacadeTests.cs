using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Modules.Multiplayer.Primitives;
using NUnit.Framework;

namespace Modules.Multiplayer.Session.Tests
{
    public sealed class ClientSessionFacadeTests
    {
        [Test]
        public void ApplySnapshot_IgnoresOlderOrEqualRevisionForSameSession()
        {
            var fixture = CreateFixture();

            fixture.Service.ApplySnapshot(Snapshot(SessionA, 5));
            fixture.Service.ApplySnapshot(Snapshot(SessionA, 4));
            fixture.Service.ApplySnapshot(Snapshot(SessionA, 5));

            Assert.That(fixture.Service.Snapshot.Revision, Is.EqualTo(5));
            Assert.That(fixture.Changes, Has.Count.EqualTo(1));
        }

        [Test]
        public void ApplySnapshot_NewSessionAcceptsLowerRevision()
        {
            var fixture = CreateFixture();
            var original = Snapshot(SessionA, 9);
            var replacement = Snapshot(SessionB, 1);

            fixture.Service.ApplySnapshot(original);
            fixture.Service.ApplySnapshot(replacement);

            Assert.That(fixture.Service.Snapshot, Is.SameAs(replacement));
            Assert.That(fixture.Changes, Is.EqualTo(new[] { original, replacement }));
        }

        [Test]
        public void ApplySnapshot_AcceptedSnapshotRaisesEventExactlyOnce()
        {
            var fixture = CreateFixture();
            var snapshot = Snapshot(SessionA, 3);

            fixture.Service.ApplySnapshot(snapshot);

            Assert.That(fixture.Changes, Has.Count.EqualTo(1));
            Assert.That(fixture.Changes[0], Is.SameAs(snapshot));
        }

        [Test]
        public void ApplySnapshot_NullSnapshotIsRejected()
        {
            var fixture = CreateFixture();

            Assert.Throws<ArgumentNullException>(() => fixture.Service.ApplySnapshot(null));
            Assert.That(fixture.Service.Snapshot, Is.Null);
            Assert.That(fixture.Changes, Is.Empty);
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task SetReadyAsync_DelegatesToTypedPublisher(bool ready)
        {
            var expected = Result<Unit, SessionError>.Failure(SessionError.InvalidPhase);
            var fixture = CreateFixture(expected);
            using var cancellation = new CancellationTokenSource();

            var actual = await fixture.Service.SetReadyAsync(ready, cancellation.Token);

            Assert.That(actual.IsFailure, Is.True);
            Assert.That(actual.Error, Is.EqualTo(expected.Error));
            Assert.That(fixture.Publisher.Calls, Has.Count.EqualTo(1));
            Assert.That(fixture.Publisher.Calls[0].Ready, Is.EqualTo(ready));
            Assert.That(fixture.Publisher.Calls[0].Token, Is.EqualTo(cancellation.Token));
        }

        [Test]
        public void Constructor_NullPublisherIsRejected()
        {
            Assert.Throws<ArgumentNullException>(() => new ClientSessionFacade(null));
        }

        private static readonly SessionId SessionA =
            new SessionId(new Guid("10000000-0000-0000-0000-000000000001"));

        private static readonly SessionId SessionB =
            new SessionId(new Guid("20000000-0000-0000-0000-000000000002"));

        private static ClientFixture CreateFixture(
            Result<Unit, SessionError>? publisherResult = null)
        {
            var publisher = new FakeClientSessionCommandPublisher(
                publisherResult ?? Result<Unit, SessionError>.Success(Unit.Value));
            var service = new ClientSessionFacade(publisher);
            var changes = new List<SessionSnapshot>();
            service.SnapshotChanged += changes.Add;
            return new ClientFixture(service, publisher, changes);
        }

        private static SessionSnapshot Snapshot(SessionId sessionId, long revision)
        {
            return new SessionSnapshot(
                sessionId,
                MatchId.None,
                MapId.None,
                SessionPhase.WaitingForPlayers,
                revision,
                false,
                Array.Empty<SessionPlayerSnapshot>());
        }

        private readonly struct ClientFixture
        {
            public ClientFixture(
                ClientSessionFacade service,
                FakeClientSessionCommandPublisher publisher,
                List<SessionSnapshot> changes)
            {
                Service = service;
                Publisher = publisher;
                Changes = changes;
            }

            public ClientSessionFacade Service { get; }
            public FakeClientSessionCommandPublisher Publisher { get; }
            public List<SessionSnapshot> Changes { get; }
        }

        private sealed class FakeClientSessionCommandPublisher : IClientSessionCommandPublisher
        {
            private readonly Result<Unit, SessionError> _result;

            public FakeClientSessionCommandPublisher(Result<Unit, SessionError> result)
            {
                _result = result;
            }

            public List<SetReadyCall> Calls { get; } = new List<SetReadyCall>();

            public ValueTask<Result<Unit, SessionError>> SetReadyAsync(
                bool ready,
                CancellationToken token)
            {
                Calls.Add(new SetReadyCall(ready, token));
                return new ValueTask<Result<Unit, SessionError>>(_result);
            }
        }

        private readonly struct SetReadyCall
        {
            public SetReadyCall(bool ready, CancellationToken token)
            {
                Ready = ready;
                Token = token;
            }

            public bool Ready { get; }
            public CancellationToken Token { get; }
        }
    }
}
