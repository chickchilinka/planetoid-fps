using Modules.Multiplayer.Primitives;

namespace Modules.Multiplayer.Session
{
    public enum SessionTelemetryKind
    {
        PhaseChanged,
        CommandRejected,
        WorldLoad,
        PlayerWorldReady,
        Spawn,
        Disconnect
    }

    public readonly struct SessionTelemetryEvent
    {
        public SessionTelemetryEvent(
            SessionTelemetryKind kind,
            SessionId sessionId,
            MatchId matchId,
            OperationId operationId,
            PlayerId playerId,
            SessionPhase phase,
            long revision,
            SessionError? error = null)
        {
            Kind = kind;
            SessionId = sessionId;
            MatchId = matchId;
            OperationId = operationId;
            PlayerId = playerId;
            Phase = phase;
            Revision = revision;
            Error = error;
        }

        public SessionTelemetryKind Kind { get; }
        public SessionId SessionId { get; }
        public MatchId MatchId { get; }
        public OperationId OperationId { get; }
        public PlayerId PlayerId { get; }
        public SessionPhase Phase { get; }
        public long Revision { get; }
        public SessionError? Error { get; }
    }

    public readonly struct MatchWorldLoadRequest
    {
        public MatchWorldLoadRequest(OperationId operationId, MatchId matchId, MapId mapId)
        {
            if (!operationId.IsValid) throw new System.ArgumentException("Operation ID must be valid.", nameof(operationId));
            if (!matchId.IsValid) throw new System.ArgumentException("Match ID must be valid.", nameof(matchId));
            if (!mapId.IsValid) throw new System.ArgumentException("Map ID must be valid.", nameof(mapId));
            OperationId = operationId;
            MatchId = matchId;
            MapId = mapId;
        }

        public OperationId OperationId { get; }
        public MatchId MatchId { get; }
        public MapId MapId { get; }
    }

    public readonly struct SpawnPlayerRequest
    {
        public SpawnPlayerRequest(
            OperationId operationId,
            MatchId matchId,
            PlayerId playerId,
            SessionConnection connection)
        {
            OperationId = operationId;
            MatchId = matchId;
            PlayerId = playerId;
            Connection = connection;
        }

        public OperationId OperationId { get; }
        public MatchId MatchId { get; }
        public PlayerId PlayerId { get; }
        public SessionConnection Connection { get; }
    }

    public readonly struct SpawnPlayerResult
    {
        public static readonly SpawnPlayerResult Completed = new SpawnPlayerResult();
    }
}
