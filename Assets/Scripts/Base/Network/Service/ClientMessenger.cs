using Base.Network.Data;
using Base.Network.Factory;
using Base.Network.Provider;
using Cysharp.Threading.Tasks;

namespace Base.Network.Service
{
    public sealed class ClientMessenger
    {
        private readonly INetworkClient _client;
        private readonly EnvelopeFactory _envelopeFactory;

        public ClientMessenger(INetworkClient client, EnvelopeFactory envelopeFactory)
        {
            _client = client;
            _envelopeFactory = envelopeFactory;
        }

        public UniTask Send<T>(in T msg) where T : struct, IMessagePayload
            => _client.SendAsync(_envelopeFactory.Create(msg, default));
    }
}