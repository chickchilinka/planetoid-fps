using System;
using Modules.Multiplayer.Primitives;

namespace Modules.Multiplayer.Spawning
{
    public enum SpawnTelemetryKind
    {
        Reserved,
        AttemptFailed,
        Spawned,
        Destroyed,
        Released
    }

    public readonly struct SpawnTelemetryEvent
    {
        public SpawnTelemetryEvent(SpawnTelemetryKind kind, PlayerId playerId, MatchId matchId, string pointId, int attempt, TimeSpan duration, SpawnError? error = null)
        {
            Kind = kind;
            PlayerId = playerId;
            MatchId = matchId;
            PointId = pointId ?? string.Empty;
            Attempt = attempt;
            Duration = duration;
            Error = error;
        }

        public SpawnTelemetryKind Kind { get; }
        public PlayerId PlayerId { get; }
        public MatchId MatchId { get; }
        public string PointId { get; }
        public int Attempt { get; }
        public TimeSpan Duration { get; }
        public SpawnError? Error { get; }
    }
}
