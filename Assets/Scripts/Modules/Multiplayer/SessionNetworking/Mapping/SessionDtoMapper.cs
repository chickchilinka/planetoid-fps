using System;
using System.Collections.Generic;
using System.IO;
using Modules.Multiplayer.Primitives;
using Modules.Multiplayer.Session;

namespace Modules.Multiplayer.Session.Networking
{
    public static class SessionDtoMapper
    {
        public static SessionSnapshotMessage ToDto(SessionSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

            var players = new SessionPlayerSnapshotMessage[snapshot.Players.Count];
            for (var i = 0; i < players.Length; i++)
            {
                var player = snapshot.Players[i];
                players[i] = new SessionPlayerSnapshotMessage
                {
                    PlayerId = player.PlayerId.Value.ToString("D"),
                    Ready = player.Ready,
                    WorldReady = player.WorldReady,
                    SpawnState = (int)player.SpawnState,
                    JoinKind = (int)player.JoinKind
                };
            }

            return new SessionSnapshotMessage
            {
                SessionId = snapshot.SessionId.Value.ToString("D"),
                MatchId = ToOptionalGuid(snapshot.MatchId.Value),
                MapId = ToOptionalGuid(snapshot.MapId.Value),
                Phase = (int)snapshot.Phase,
                Revision = snapshot.Revision,
                ServerWorldReady = snapshot.ServerWorldReady,
                Players = players
            };
        }

        public static SessionSnapshot ToDomain(SessionSnapshotMessage message)
        {
            var sessionId = new SessionId(ParseRequiredGuid(message.SessionId, nameof(message.SessionId)));
            var matchId = new MatchId(ParseOptionalGuid(message.MatchId, nameof(message.MatchId)));
            var mapId = new MapId(ParseOptionalGuid(message.MapId, nameof(message.MapId)));
            var phase = ParseEnum<SessionPhase>(message.Phase, nameof(message.Phase));
            if (message.Revision < 0) throw Invalid(nameof(message.Revision), "must be non-negative");
            if (message.Players == null) throw Invalid(nameof(message.Players), "must not be null");

            var playerIds = new HashSet<PlayerId>();
            var players = new SessionPlayerSnapshot[message.Players.Length];
            for (var i = 0; i < message.Players.Length; i++)
            {
                var wirePlayer = message.Players[i];
                var playerId = new PlayerId(ParseRequiredGuid(
                    wirePlayer.PlayerId, $"{nameof(message.Players)}[{i}].{nameof(wirePlayer.PlayerId)}"));
                if (!playerIds.Add(playerId))
                    throw Invalid(nameof(message.Players), $"contains duplicate player ID '{wirePlayer.PlayerId}'");

                players[i] = new SessionPlayerSnapshot(
                    playerId,
                    wirePlayer.Ready,
                    wirePlayer.WorldReady,
                    ParseEnum<SpawnState>(wirePlayer.SpawnState,
                        $"{nameof(message.Players)}[{i}].{nameof(wirePlayer.SpawnState)}"),
                    ParseEnum<SessionJoinKind>(wirePlayer.JoinKind,
                        $"{nameof(message.Players)}[{i}].{nameof(wirePlayer.JoinKind)}"));
            }

            return new SessionSnapshot(
                sessionId,
                matchId,
                mapId,
                phase,
                message.Revision,
                message.ServerWorldReady,
                players);
        }

        public static JoinAcceptedMessage ToJoinAccepted(PlayerId playerId, SessionSnapshot snapshot)
        {
            if (!playerId.IsValid) throw new ArgumentException("Player ID must be valid.", nameof(playerId));
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

            return new JoinAcceptedMessage
            {
                PlayerId = playerId.Value.ToString("D"),
                SessionId = snapshot.SessionId.Value.ToString("D"),
                Revision = snapshot.Revision
            };
        }

        public static PlayerId ToPlayerId(JoinAcceptedMessage message)
        {
            return new PlayerId(ParseRequiredGuid(message.PlayerId, nameof(message.PlayerId)));
        }

        public static SessionId ToSessionId(JoinAcceptedMessage message)
        {
            return new SessionId(ParseRequiredGuid(message.SessionId, nameof(message.SessionId)));
        }

        public static long ToRevision(JoinAcceptedMessage message)
        {
            if (message.Revision < 0) throw Invalid(nameof(message.Revision), "must be non-negative");
            return message.Revision;
        }

        public static SessionCommandRejectedMessage ToDto(SessionError error)
        {
            ValidateEnum(error, nameof(error));
            return new SessionCommandRejectedMessage { Error = (int)error };
        }

        public static SessionError ToDomain(SessionCommandRejectedMessage message)
        {
            return ParseEnum<SessionError>(message.Error, nameof(message.Error));
        }

        private static string ToOptionalGuid(Guid value)
        {
            return value == Guid.Empty ? null : value.ToString("D");
        }

        private static Guid ParseRequiredGuid(string value, string field)
        {
            if (string.IsNullOrWhiteSpace(value) || !Guid.TryParseExact(value, "D", out var parsed) ||
                parsed == Guid.Empty)
                throw Invalid(field, "must be a non-empty GUID in D format");
            return parsed;
        }

        private static Guid ParseOptionalGuid(string value, string field)
        {
            if (string.IsNullOrEmpty(value)) return Guid.Empty;
            if (string.IsNullOrWhiteSpace(value) || !Guid.TryParseExact(value, "D", out var parsed) ||
                parsed == Guid.Empty)
                throw Invalid(field, "must be null/empty or a non-empty GUID in D format");
            return parsed;
        }

        private static TEnum ParseEnum<TEnum>(int value, string field) where TEnum : struct, Enum
        {
            if (!Enum.IsDefined(typeof(TEnum), value))
                throw Invalid(field, $"contains unknown {typeof(TEnum).Name} value {value}");
            return (TEnum)Enum.ToObject(typeof(TEnum), value);
        }

        private static void ValidateEnum<TEnum>(TEnum value, string field) where TEnum : struct, Enum
        {
            if (!Enum.IsDefined(typeof(TEnum), value))
                throw new ArgumentOutOfRangeException(field, value, $"Unknown {typeof(TEnum).Name} value.");
        }

        private static InvalidDataException Invalid(string field, string reason)
        {
            return new InvalidDataException($"Session wire field '{field}' {reason}.");
        }
    }
}
