using Base.Network.Data;
using Base.Network.Handler;
using Cysharp.Threading.Tasks;

namespace Demos.NetworkingDemo.Scripts
{
    public class DemoClientRequestHandler: IClientRequestHandler<DemoRequest, DemoRequest>
    {
        public UniTask<DemoRequest> HandleAsync(in DemoRequest req, in MessageContext ctx)
        {
            return new UniTask<DemoRequest>(new DemoRequest()
            {
                Value = req.Value - 1,
            });
        }
    }
}