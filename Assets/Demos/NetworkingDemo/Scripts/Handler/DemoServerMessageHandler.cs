using Base.Network.Data;
using Base.Network.Handler;
using Base.Network.Model;
using Base.Network.Provider;
using Base.Network.Service;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Demos.NetworkingDemo.Scripts
{
    public class DemoServerMessageHandler: IConnectionMessageHandler<DemoMessage>
    {
        private ServerMessenger _serverMessenger;

        public DemoServerMessageHandler(ServerMessenger serverMessenger)
        {
            _serverMessenger = serverMessenger;
        }

        public async UniTask HandleAsync(DemoMessage msg, ConnectionId from, MessageContext ctx)
        {
            Debug.Log($"Received Message: {msg.Message}");
            await _serverMessenger.To(from, new DemoMessage()
            {
                Message = "Hello World!"
            });
        }
    }
}