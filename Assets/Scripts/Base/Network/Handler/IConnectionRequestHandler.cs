using Base.Network.Data;
using Cysharp.Threading.Tasks;

namespace Base.Network.Handler
{
    public interface IConnectionRequestHandler<TReq,TRes>
        where TReq: struct, IMessagePayload where TRes: struct, IMessagePayload
    {
        UniTask<TRes> HandleAsync(TReq req, ConnectionId from, MessageContext ctx);
    }
}