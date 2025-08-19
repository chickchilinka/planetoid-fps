using Base.Network.Data;

namespace Base.Network.Factory
{
    public class MessageIdGenerator
    {
        private int _id;
        public MessageId Next() => new((uint)System.Threading.Interlocked.Increment(ref _id));
    }
}