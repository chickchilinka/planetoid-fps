namespace Modules.Multiplayer.Session.Networking
{
    public static class SessionMessageTypeIds
    {
        public const ushort SetReady = 1000;
        public const ushort JoinAccepted = 1001;
        public const ushort Snapshot = 1002;
        public const ushort CommandRejected = 1003;
    }
}
