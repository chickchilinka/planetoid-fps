using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Modules.Multiplayer.Primitives;

namespace Modules.Multiplayer.Session
{
    public readonly struct SessionPlayerSnapshot : IEquatable<SessionPlayerSnapshot>
    {
        public SessionPlayerSnapshot(
            PlayerId playerId,
            bool ready,
            bool worldReady,
            SpawnState spawnState,
            SessionJoinKind joinKind)
        {
            PlayerId = playerId;
            Ready = ready;
            WorldReady = worldReady;
            SpawnState = spawnState;
            JoinKind = joinKind;
        }

        public PlayerId PlayerId { get; }
        public bool Ready { get; }
        public bool WorldReady { get; }
        public SpawnState SpawnState { get; }
        public SessionJoinKind JoinKind { get; }

        public bool Equals(SessionPlayerSnapshot other)
        {
            return PlayerId == other.PlayerId && Ready == other.Ready &&
                   WorldReady == other.WorldReady && SpawnState == other.SpawnState && JoinKind == other.JoinKind;
        }

        public override bool Equals(object obj) => obj is SessionPlayerSnapshot other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(PlayerId, Ready, WorldReady, SpawnState, JoinKind);
        public static bool operator ==(SessionPlayerSnapshot left, SessionPlayerSnapshot right) => left.Equals(right);
        public static bool operator !=(SessionPlayerSnapshot left, SessionPlayerSnapshot right) => !left.Equals(right);
    }

    public sealed class SessionSnapshot : IEquatable<SessionSnapshot>
    {
        private readonly ReadOnlyCollection<SessionPlayerSnapshot> _players;

        public SessionSnapshot(
            SessionId sessionId,
            MatchId matchId,
            MapId mapId,
            SessionPhase phase,
            long revision,
            bool serverWorldReady,
            IEnumerable<SessionPlayerSnapshot> players)
        {
            if (!sessionId.IsValid) throw new ArgumentException("Session ID must be valid.", nameof(sessionId));
            if (revision < 0) throw new ArgumentOutOfRangeException(nameof(revision));

            SessionId = sessionId;
            MatchId = matchId;
            MapId = mapId;
            Phase = phase;
            Revision = revision;
            ServerWorldReady = serverWorldReady;
            var copy = players?.ToArray() ?? throw new ArgumentNullException(nameof(players));
            if (copy.Select(value => value.PlayerId).Distinct().Count() != copy.Length)
                throw new ArgumentException("Player IDs must be unique.", nameof(players));
            _players = Array.AsReadOnly(copy);
        }

        public SessionId SessionId { get; }
        public MatchId MatchId { get; }
        public MapId MapId { get; }
        public SessionPhase Phase { get; }
        public long Revision { get; }
        public bool ServerWorldReady { get; }
        public IReadOnlyList<SessionPlayerSnapshot> Players => _players;

        public SessionPlayerSnapshot Player(PlayerId playerId)
        {
            for (var i = 0; i < _players.Count; i++)
                if (_players[i].PlayerId == playerId)
                    return _players[i];
            throw new KeyNotFoundException($"Player {playerId} is not in the snapshot.");
        }

        public bool Equals(SessionSnapshot other)
        {
            return other != null && SessionId == other.SessionId && MatchId == other.MatchId &&
                   MapId == other.MapId && Phase == other.Phase && Revision == other.Revision &&
                   ServerWorldReady == other.ServerWorldReady && _players.SequenceEqual(other._players);
        }

        public override bool Equals(object obj) => Equals(obj as SessionSnapshot);

        public override int GetHashCode()
        {
            var hash = HashCode.Combine(SessionId, MatchId, MapId, Phase, Revision, ServerWorldReady);
            for (var i = 0; i < _players.Count; i++) hash = HashCode.Combine(hash, _players[i]);
            return hash;
        }
    }
}
