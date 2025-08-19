using Base.Network.Data;
using Cysharp.Threading.Tasks;

namespace Base.Network.Handler
{
    public interface IClientMessageHandler<T> where T: IMessagePayload
    {
        UniTask HandleAsync(in T msg, in MessageContext ctx);
    }
}