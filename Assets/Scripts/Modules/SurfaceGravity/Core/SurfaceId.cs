using System;

namespace Modules.SurfaceGravity.Core
{
    public readonly struct SurfaceId : IEquatable<SurfaceId>
    {
        public static readonly SurfaceId None = new SurfaceId(string.Empty);

        public SurfaceId(string value)
        {
            Value = value ?? string.Empty;
        }

        public string Value { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(SurfaceId other)
        {
            return string.Equals(
                Value ?? string.Empty,
                other.Value ?? string.Empty,
                StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is SurfaceId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        }

        public override string ToString()
        {
            return Value ?? string.Empty;
        }

        public static bool operator ==(SurfaceId left, SurfaceId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SurfaceId left, SurfaceId right)
        {
            return !left.Equals(right);
        }
    }
}
