using Base.Network.Data;
using MessagePack;

namespace Modules.Multiplayer.Session.Networking
{
    /// <summary>Connection-derived acknowledgement for a rejection; it deliberately carries no identity.</summary>
    [MessagePackObject]
    public struct RejectionAcknowledgedMessage : IMessagePayload
    {
    }
}
