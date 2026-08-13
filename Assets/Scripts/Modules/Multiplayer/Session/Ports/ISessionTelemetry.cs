namespace Modules.Multiplayer.Session
{
    public interface ISessionTelemetry
    {
        void Record(SessionTelemetryEvent value);
    }

    public sealed class NullSessionTelemetry : ISessionTelemetry
    {
        public static readonly NullSessionTelemetry Instance = new NullSessionTelemetry();

        private NullSessionTelemetry()
        {
        }

        public void Record(SessionTelemetryEvent value)
        {
        }
    }
}
