using System;
using Modules.Multiplayer.Primitives;

namespace Modules.Multiplayer.Session
{
    public sealed class SessionConfiguration
    {
        public SessionConfiguration(
            int maxPlayers,
            int minPlayers,
            MapId mapId,
            TimeSpan worldLoadTimeout,
            TimeSpan playerLoadTimeout)
        {
            if (maxPlayers != 10)
                throw new ArgumentOutOfRangeException(nameof(maxPlayers), "The first slice supports exactly 10 players.");
            if (minPlayers != 2)
                throw new ArgumentOutOfRangeException(nameof(minPlayers), "The first slice starts with exactly 2 minimum players.");
            if (!mapId.IsValid) throw new ArgumentException("Map ID must be valid.", nameof(mapId));
            if (worldLoadTimeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(worldLoadTimeout), "Timeout must be positive.");
            if (playerLoadTimeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(playerLoadTimeout), "Timeout must be positive.");

            MaxPlayers = maxPlayers;
            MinPlayers = minPlayers;
            MapId = mapId;
            WorldLoadTimeout = worldLoadTimeout;
            PlayerLoadTimeout = playerLoadTimeout;
        }

        public int MaxPlayers { get; }
        public int MinPlayers { get; }
        public MapId MapId { get; }
        public TimeSpan WorldLoadTimeout { get; }
        public TimeSpan PlayerLoadTimeout { get; }
    }
}
