using System;
using System.IO;
using System.Linq;
using Base.Network.Data;
using MessagePack;
using Modules.Multiplayer.Session;
using NUnit.Framework;

namespace Modules.Multiplayer.Session.Networking.Tests
{
    public sealed class SessionDtoMapperTests
    {
        [Test]
        public void MessageTypeIds_AreStableAndDistinct()
        {
            Assert.That(SessionMessageTypeIds.SetReady, Is.EqualTo(1000));
            Assert.That(SessionMessageTypeIds.JoinAccepted, Is.EqualTo(1001));
            Assert.That(SessionMessageTypeIds.Snapshot, Is.EqualTo(1002));
            Assert.That(SessionMessageTypeIds.CommandRejected, Is.EqualTo(1003));
            Assert.That(new[]
            {
                SessionMessageTypeIds.SetReady,
                SessionMessageTypeIds.JoinAccepted,
                SessionMessageTypeIds.Snapshot,
                SessionMessageTypeIds.CommandRejected
            }.Distinct().Count(), Is.EqualTo(4));
        }

        [Test]
        public void SetReadyMessage_ContainsOnlyReadyFlag()
        {
            var properties = typeof(SetReadyMessage).GetProperties().Select(property => property.Name).ToArray();

            Assert.That(properties, Is.EqualTo(new[] { "Ready" }));
            Assert.That(typeof(IMessagePayload).IsAssignableFrom(typeof(SetReadyMessage)), Is.True);
        }

        [TestCase(typeof(SetReadyMessage), 1)]
        [TestCase(typeof(JoinAcceptedMessage), 3)]
        [TestCase(typeof(SessionSnapshotMessage), 7)]
        [TestCase(typeof(SessionPlayerSnapshotMessage), 5)]
        [TestCase(typeof(SessionCommandRejectedMessage), 1)]
        public void WireDtos_UseContiguousNumericMessagePackKeys(Type dtoType, int propertyCount)
        {
            Assert.That(dtoType.GetCustomAttributes(typeof(MessagePackObjectAttribute), false), Has.Length.EqualTo(1));
            var properties = dtoType.GetProperties();
            Assert.That(properties, Has.Length.EqualTo(propertyCount));
            var keys = properties.Select(property =>
                    ((KeyAttribute)property.GetCustomAttributes(typeof(KeyAttribute), false).Single()).IntKey)
                .OrderBy(value => value)
                .ToArray();

            Assert.That(keys, Is.EqualTo(Enumerable.Range(0, propertyCount).ToArray()));
        }

        [TestCase(typeof(SetReadyMessage))]
        [TestCase(typeof(JoinAcceptedMessage))]
        [TestCase(typeof(SessionSnapshotMessage))]
        [TestCase(typeof(SessionCommandRejectedMessage))]
        public void TopLevelMessages_ImplementPayloadMarker(Type dtoType)
        {
            Assert.That(typeof(IMessagePayload).IsAssignableFrom(dtoType), Is.True);
        }

        [Test]
        public void Snapshot_RoundTripsAllAuthoritativeFields()
        {
            var expected = SessionNetworkingFixtures.PlayingWithThreePlayers();

            var actual = SessionDtoMapper.ToDomain(SessionDtoMapper.ToDto(expected));

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Snapshot_MessagePackSerialization_PreservesDomainRoundTrip()
        {
            var expected = SessionNetworkingFixtures.PlayingWithThreePlayers();
            var bytes = MessagePackSerializer.Serialize(SessionDtoMapper.ToDto(expected));
            var wire = MessagePackSerializer.Deserialize<SessionSnapshotMessage>(bytes);

            Assert.That(SessionDtoMapper.ToDomain(wire), Is.EqualTo(expected));
        }

        [Test]
        public void Snapshot_EmptyOptionalMatchAndMap_RoundTripsAsNone()
        {
            var expected = new SessionSnapshot(
                SessionNetworkingFixtures.Session("10000000-0000-0000-0000-000000000001"),
                Modules.Multiplayer.Primitives.MatchId.None,
                Modules.Multiplayer.Primitives.MapId.None,
                SessionPhase.WaitingForPlayers,
                0,
                false,
                Array.Empty<SessionPlayerSnapshot>());

            var actual = SessionDtoMapper.ToDomain(SessionDtoMapper.ToDto(expected));

            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("not-a-guid")]
        [TestCase("00000000-0000-0000-0000-000000000000")]
        public void Snapshot_InvalidSessionId_IsRejected(string sessionId)
        {
            var message = SessionDtoMapper.ToDto(SessionNetworkingFixtures.PlayingWithThreePlayers());
            message.SessionId = sessionId;

            Assert.Throws<InvalidDataException>(() => SessionDtoMapper.ToDomain(message));
        }

        [TestCase("not-a-guid")]
        [TestCase("00000000-0000-0000-0000-000000000000")]
        public void Snapshot_InvalidPlayerId_IsRejected(string playerId)
        {
            var message = SessionDtoMapper.ToDto(SessionNetworkingFixtures.PlayingWithThreePlayers());
            message.Players[0].PlayerId = playerId;

            Assert.Throws<InvalidDataException>(() => SessionDtoMapper.ToDomain(message));
        }

        [TestCase("not-a-guid")]
        [TestCase("00000000-0000-0000-0000-000000000000")]
        public void Snapshot_InvalidOptionalGuidWhenPresent_IsRejected(string value)
        {
            var message = SessionDtoMapper.ToDto(SessionNetworkingFixtures.PlayingWithThreePlayers());
            message.MatchId = value;

            Assert.Throws<InvalidDataException>(() => SessionDtoMapper.ToDomain(message));
        }

        [TestCase(-1)]
        [TestCase(5)]
        [TestCase(int.MaxValue)]
        public void Snapshot_InvalidPhaseNumber_IsRejected(int phase)
        {
            var message = SessionDtoMapper.ToDto(SessionNetworkingFixtures.PlayingWithThreePlayers());
            message.Phase = phase;

            Assert.Throws<InvalidDataException>(() => SessionDtoMapper.ToDomain(message));
        }

        [TestCase(-1)]
        [TestCase(3)]
        [TestCase(int.MaxValue)]
        public void Snapshot_InvalidSpawnStateNumber_IsRejected(int spawnState)
        {
            var message = SessionDtoMapper.ToDto(SessionNetworkingFixtures.PlayingWithThreePlayers());
            message.Players[0].SpawnState = spawnState;

            Assert.Throws<InvalidDataException>(() => SessionDtoMapper.ToDomain(message));
        }

        [TestCase(-1)]
        [TestCase(2)]
        [TestCase(int.MaxValue)]
        public void Snapshot_InvalidJoinKindNumber_IsRejected(int joinKind)
        {
            var message = SessionDtoMapper.ToDto(SessionNetworkingFixtures.PlayingWithThreePlayers());
            message.Players[0].JoinKind = joinKind;

            Assert.Throws<InvalidDataException>(() => SessionDtoMapper.ToDomain(message));
        }

        [Test]
        public void Snapshot_NegativeRevision_IsRejected()
        {
            var message = SessionDtoMapper.ToDto(SessionNetworkingFixtures.PlayingWithThreePlayers());
            message.Revision = -1;

            Assert.Throws<InvalidDataException>(() => SessionDtoMapper.ToDomain(message));
        }

        [Test]
        public void Snapshot_NullPlayers_IsRejected()
        {
            var message = SessionDtoMapper.ToDto(SessionNetworkingFixtures.PlayingWithThreePlayers());
            message.Players = null;

            Assert.Throws<InvalidDataException>(() => SessionDtoMapper.ToDomain(message));
        }

        [Test]
        public void Snapshot_DuplicatePlayers_AreRejected()
        {
            var message = SessionDtoMapper.ToDto(SessionNetworkingFixtures.PlayingWithThreePlayers());
            message.Players[1].PlayerId = message.Players[0].PlayerId;

            Assert.Throws<InvalidDataException>(() => SessionDtoMapper.ToDomain(message));
        }

        [Test]
        public void JoinAccepted_RoundTripsIdentityAndRevision()
        {
            var snapshot = SessionNetworkingFixtures.PlayingWithThreePlayers();
            var playerId = snapshot.Players[0].PlayerId;

            var message = SessionDtoMapper.ToJoinAccepted(playerId, snapshot);

            Assert.That(SessionDtoMapper.ToPlayerId(message), Is.EqualTo(playerId));
            Assert.That(SessionDtoMapper.ToSessionId(message), Is.EqualTo(snapshot.SessionId));
            Assert.That(SessionDtoMapper.ToRevision(message), Is.EqualTo(snapshot.Revision));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("invalid")]
        [TestCase("00000000-0000-0000-0000-000000000000")]
        public void JoinAccepted_InvalidPlayerId_IsRejected(string value)
        {
            var message = SessionDtoMapper.ToJoinAccepted(
                SessionNetworkingFixtures.PlayingWithThreePlayers().Players[0].PlayerId,
                SessionNetworkingFixtures.PlayingWithThreePlayers());
            message.PlayerId = value;

            Assert.Throws<InvalidDataException>(() => SessionDtoMapper.ToPlayerId(message));
        }

        [Test]
        public void JoinAccepted_NegativeRevision_IsRejected()
        {
            var snapshot = SessionNetworkingFixtures.PlayingWithThreePlayers();
            var message = SessionDtoMapper.ToJoinAccepted(snapshot.Players[0].PlayerId, snapshot);
            message.Revision = -1;

            Assert.Throws<InvalidDataException>(() => SessionDtoMapper.ToRevision(message));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("invalid")]
        [TestCase("00000000-0000-0000-0000-000000000000")]
        public void JoinAccepted_InvalidSessionId_IsRejected(string value)
        {
            var snapshot = SessionNetworkingFixtures.PlayingWithThreePlayers();
            var message = SessionDtoMapper.ToJoinAccepted(snapshot.Players[0].PlayerId, snapshot);
            message.SessionId = value;

            Assert.Throws<InvalidDataException>(() => SessionDtoMapper.ToSessionId(message));
        }

        [TestCase(-1)]
        [TestCase(9)]
        [TestCase(int.MaxValue)]
        public void CommandRejected_InvalidErrorNumber_IsRejected(int error)
        {
            Assert.Throws<InvalidDataException>(() => SessionDtoMapper.ToDomain(
                new SessionCommandRejectedMessage { Error = error }));
        }

        [Test]
        public void CommandRejected_RoundTripsStableErrorCode()
        {
            const SessionError expected = SessionError.PlayerLoadTimeout;

            Assert.That(SessionDtoMapper.ToDomain(SessionDtoMapper.ToDto(expected)), Is.EqualTo(expected));
        }
    }
}
