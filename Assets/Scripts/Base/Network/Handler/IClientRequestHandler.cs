using Base.Network.Data;
using Cysharp.Threading.Tasks;

namespace Base.Network.Handler
{
    public interface IClientRequestHandler<TReq,TRes>
        where TReq: struct, IMessagePayload where TRes: struct, IMessagePayload
    {
        UniTask<TRes> HandleAsync(in TReq req, in MessageContext ctx);
    }

}