using Base.Network.Data;
using Base.Network.Handler;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Demos.NetworkingDemo.Scripts
{
    public class DemoClientMessageHandler : IClientMessageHandler<DemoMessage>
    {
        public UniTask HandleAsync(in DemoMessage msg, in MessageContext ctx)
        {
            Debug.Log(
                $"Received Message: {msg.Message}. Tick: {ctx.ServerTick}. Source: {ctx.Source}. MessageId: {ctx.CorrelationId}");
            return UniTask.CompletedTask;
        }
    }
}