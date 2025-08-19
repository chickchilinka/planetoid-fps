using Base.Network.Data;
using MessagePack;

namespace Demos.NetworkingDemo.Scripts
{
    [MessagePackObject]
    public struct DemoMessage: IMessagePayload
    {
        [Key("message")]
        public string Message { get; set; }
    }
}