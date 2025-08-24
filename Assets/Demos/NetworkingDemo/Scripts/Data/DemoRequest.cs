using Base.Network.Data;
using MessagePack;

namespace Demos.NetworkingDemo.Scripts
{
    [MessagePackObject]
    public struct DemoRequest: IMessagePayload
    {
        [Key(0)]
        public int Value { get; set; }
    }
}