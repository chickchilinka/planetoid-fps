using Base.Network.Data;
using Base.Network.Handler;
using Cysharp.Threading.Tasks;

namespace Demos.NetworkingDemo.Scripts
{
    public class DemoServerRequestHandler: IConnectionRequestHandler<DemoRequest, DemoRequest>
    {
        public UniTask<DemoRequest> HandleAsync(DemoRequest req, ConnectionId from, MessageContext ctx)
        {
            return UniTask.FromResult(new DemoRequest()
            {
                Value = req.Value + 1
            });
        }
    }
}