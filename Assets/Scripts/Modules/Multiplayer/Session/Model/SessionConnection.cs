using System;

namespace Modules.Multiplayer.Session
{
    public readonly struct SessionConnection : IEquatable<SessionConnection>
    {
        public static readonly SessionConnection None = new SessionConnection(Guid.Empty);

        public SessionConnection(Guid value)
        {
            Value = value;
        }

        public Guid Value { get; }
        public bool IsValid => Value != Guid.Empty;

        public bool Equals(SessionConnection other) => Value.Equals(other.Value);
        public override bool Equals(object obj) => obj is SessionConnection other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString("N");
        public static bool operator ==(SessionConnection left, SessionConnection right) => left.Equals(right);
        public static bool operator !=(SessionConnection left, SessionConnection right) => !left.Equals(right);
    }
}
