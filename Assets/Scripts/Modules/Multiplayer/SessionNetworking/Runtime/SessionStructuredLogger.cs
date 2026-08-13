using System;
using Modules.Multiplayer.Session;
using UnityEngine;

namespace Modules.Multiplayer.Session.Networking
{
    public sealed class SessionStructuredLogger : ISessionTelemetry
    {
        public void Record(SessionTelemetryEvent value)
        {
            Debug.Log($"{{\"timestampUtc\":\"{DateTime.UtcNow:O}\",\"kind\":\"{value.Kind}\",\"sessionId\":\"{value.SessionId}\",\"matchId\":\"{value.MatchId}\",\"operationId\":\"{value.OperationId}\",\"playerId\":\"{value.PlayerId}\",\"phase\":\"{value.Phase}\",\"revision\":{value.Revision},\"error\":\"{value.Error?.ToString() ?? string.Empty}\"}}");
        }
    }
}
