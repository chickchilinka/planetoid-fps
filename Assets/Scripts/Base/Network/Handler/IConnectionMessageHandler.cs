using Base.Network.Data;
using Base.Network.Model;
using Cysharp.Threading.Tasks;

namespace Base.Network.Handler
{
    public interface IConnectionMessageHandler<in T> where T : struct, IMessagePayload
    {
        UniTask HandleAsync(T msg, ConnectionId from, MessageContext ctx);
    }
}