using System;

namespace Modules.Multiplayer.Spawning
{
    public readonly struct SpawnPose : IEquatable<SpawnPose>
    {
        public SpawnPose(float x, float y, float z, float rotationX, float rotationY, float rotationZ, float rotationW)
        {
            X = x;
            Y = y;
            Z = z;
            RotationX = rotationX;
            RotationY = rotationY;
            RotationZ = rotationZ;
            RotationW = rotationW;
        }

        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public float RotationX { get; }
        public float RotationY { get; }
        public float RotationZ { get; }
        public float RotationW { get; }

        public bool Equals(SpawnPose other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z)
                && RotationX.Equals(other.RotationX) && RotationY.Equals(other.RotationY)
                && RotationZ.Equals(other.RotationZ) && RotationW.Equals(other.RotationW);
        }

        public override bool Equals(object obj)
        {
            return obj is SpawnPose other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = X.GetHashCode();
                hashCode = (hashCode * 397) ^ Y.GetHashCode();
                hashCode = (hashCode * 397) ^ Z.GetHashCode();
                hashCode = (hashCode * 397) ^ RotationX.GetHashCode();
                hashCode = (hashCode * 397) ^ RotationY.GetHashCode();
                hashCode = (hashCode * 397) ^ RotationZ.GetHashCode();
                return (hashCode * 397) ^ RotationW.GetHashCode();
            }
        }

        public static bool operator ==(SpawnPose left, SpawnPose right) => left.Equals(right);
        public static bool operator !=(SpawnPose left, SpawnPose right) => !left.Equals(right);
    }
}
