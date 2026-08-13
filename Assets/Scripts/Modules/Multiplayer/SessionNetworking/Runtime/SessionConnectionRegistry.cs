using System;
using System.Collections.Generic;
using Base.Network.Data;
using Modules.Multiplayer.Primitives;
using Modules.Multiplayer.Session;

namespace Modules.Multiplayer.Session.Networking
{
    /// <summary>Maps transport connections to opaque session identities without leaking transport IDs into Session.</summary>
    public sealed class SessionConnectionRegistry
    {
        private readonly Dictionary<int, SessionConnection> _sessionsByConnection = new();
        private readonly Dictionary<SessionConnection, ConnectionId> _connectionsBySession = new();
        private readonly Dictionary<SessionConnection, PlayerId> _playersBySession = new();
        private readonly Dictionary<PlayerId, SessionConnection> _sessionsByPlayer = new();

        public SessionConnection Register(ConnectionId connectionId)
        {
            if (_sessionsByConnection.ContainsKey(connectionId.Value))
                throw new InvalidOperationException($"Connection {connectionId.Value} is already registered.");

            var sessionConnection = new SessionConnection(Guid.NewGuid());
            _sessionsByConnection.Add(connectionId.Value, sessionConnection);
            _connectionsBySession.Add(sessionConnection, connectionId);
            return sessionConnection;
        }

        public void AttachPlayer(SessionConnection connection, PlayerId playerId)
        {
            if (!connection.IsValid) throw new ArgumentException("Session connection must be valid.", nameof(connection));
            if (!playerId.IsValid) throw new ArgumentException("Player ID must be valid.", nameof(playerId));
            if (!_connectionsBySession.ContainsKey(connection))
                throw new InvalidOperationException("Session connection is not registered.");
            if (_playersBySession.ContainsKey(connection) || _sessionsByPlayer.ContainsKey(playerId))
                throw new InvalidOperationException("Session connection or player is already attached.");

            _playersBySession.Add(connection, playerId);
            _sessionsByPlayer.Add(playerId, connection);
        }

        public bool TryGetSession(ConnectionId connectionId, out SessionConnection connection)
            => _sessionsByConnection.TryGetValue(connectionId.Value, out connection);

        public bool TryGetPlayer(ConnectionId connectionId, out PlayerId playerId)
        {
            if (_sessionsByConnection.TryGetValue(connectionId.Value, out var session))
                return _playersBySession.TryGetValue(session, out playerId);

            playerId = PlayerId.None;
            return false;
        }

        public bool TryGetTransport(SessionConnection connection, out ConnectionId connectionId)
            => _connectionsBySession.TryGetValue(connection, out connectionId);

        public bool TryGetConnection(PlayerId playerId, out ConnectionId connectionId)
        {
            if (_sessionsByPlayer.TryGetValue(playerId, out var session))
                return _connectionsBySession.TryGetValue(session, out connectionId);

            connectionId = default;
            return false;
        }

        public bool Unregister(ConnectionId connectionId, out PlayerId playerId)
        {
            if (!_sessionsByConnection.Remove(connectionId.Value, out var session))
            {
                playerId = PlayerId.None;
                return false;
            }

            _connectionsBySession.Remove(session);
            if (_playersBySession.Remove(session, out playerId))
                _sessionsByPlayer.Remove(playerId);
            else
                playerId = PlayerId.None;
            return true;
        }

        public ConnectionId[] AttachedConnections()
        {
            var result = new ConnectionId[_playersBySession.Count];
            var index = 0;
            foreach (var session in _playersBySession.Keys)
                result[index++] = _connectionsBySession[session];
            return result;
        }
    }
}
