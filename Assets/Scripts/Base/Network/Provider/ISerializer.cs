using System;
using Base.Network.Data;

namespace Base.Network.Provider
{
    public interface ISerializer
    {
        ArraySegment<byte> Serialize<T>(in T value) where T : struct, IMessagePayload;
        T Deserialize<T>(ArraySegment<byte> bytes) where T : struct, IMessagePayload;
    }
}