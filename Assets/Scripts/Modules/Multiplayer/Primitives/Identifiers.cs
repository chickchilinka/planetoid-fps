using System;

namespace Modules.Multiplayer.Primitives
{
    public readonly struct PlayerId : IEquatable<PlayerId>
    {
        public static readonly PlayerId None = new PlayerId(Guid.Empty);

        public PlayerId(Guid value)
        {
            Value = value;
        }

        public Guid Value { get; }

        public bool IsValid => Value != Guid.Empty;

        public bool Equals(PlayerId other)
        {
            return Value.Equals(other.Value);
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value.ToString("N");
        }

        public static bool operator ==(PlayerId left, PlayerId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PlayerId left, PlayerId right)
        {
            return !left.Equals(right);
        }
    }

    public readonly struct SessionId : IEquatable<SessionId>
    {
        public static readonly SessionId None = new SessionId(Guid.Empty);

        public SessionId(Guid value)
        {
            Value = value;
        }

        public Guid Value { get; }

        public bool IsValid => Value != Guid.Empty;

        public bool Equals(SessionId other)
        {
            return Value.Equals(other.Value);
        }

        public override bool Equals(object obj)
        {
            return obj is SessionId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value.ToString("N");
        }

        public static bool operator ==(SessionId left, SessionId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SessionId left, SessionId right)
        {
            return !left.Equals(right);
        }
    }

    public readonly struct MatchId : IEquatable<MatchId>
    {
        public static readonly MatchId None = new MatchId(Guid.Empty);

        public MatchId(Guid value)
        {
            Value = value;
        }

        public Guid Value { get; }

        public bool IsValid => Value != Guid.Empty;

        public bool Equals(MatchId other)
        {
            return Value.Equals(other.Value);
        }

        public override bool Equals(object obj)
        {
            return obj is MatchId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value.ToString("N");
        }

        public static bool operator ==(MatchId left, MatchId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MatchId left, MatchId right)
        {
            return !left.Equals(right);
        }
    }

    public readonly struct MapId : IEquatable<MapId>
    {
        public static readonly MapId None = new MapId(Guid.Empty);

        public MapId(Guid value)
        {
            Value = value;
        }

        public Guid Value { get; }

        public bool IsValid => Value != Guid.Empty;

        public bool Equals(MapId other)
        {
            return Value.Equals(other.Value);
        }

        public override bool Equals(object obj)
        {
            return obj is MapId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value.ToString("N");
        }

        public static bool operator ==(MapId left, MapId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MapId left, MapId right)
        {
            return !left.Equals(right);
        }
    }

    public readonly struct OperationId : IEquatable<OperationId>
    {
        public static readonly OperationId None = new OperationId(Guid.Empty);

        public OperationId(Guid value)
        {
            Value = value;
        }

        public Guid Value { get; }

        public bool IsValid => Value != Guid.Empty;

        public bool Equals(OperationId other)
        {
            return Value.Equals(other.Value);
        }

        public override bool Equals(object obj)
        {
            return obj is OperationId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value.ToString("N");
        }

        public static bool operator ==(OperationId left, OperationId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(OperationId left, OperationId right)
        {
            return !left.Equals(right);
        }
    }
}
