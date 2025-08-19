using System;
using Base.Network.Data;
using Base.Network.Provider;
using Cysharp.Threading.Tasks;
using Features.FishNetworking.Impl.Data;
using Features.FishNetworking.Impl.Utils;
using FishNet.Managing;
using FishNet.Transporting;
using UniRx;
using Channel = FishNet.Transporting.Channel;

namespace Features.FishNetworking.Impl.Adapters
{
    public sealed class FishNetClientAdapter : INetworkClient, IDisposable
    {
        private readonly NetworkManager _networkManager;
        private readonly Subject<Envelope> _onMessage = new();
        private readonly Subject<Unit> _onConnected = new();
        private readonly Subject<DisconnectReason> _onDisconnected = new();

        public FishNetClientAdapter(NetworkManager networkManager)
        {
            _networkManager = networkManager;
        }

        public bool IsConnected { get; private set; }
        public IObservable<Envelope> OnMessage => _onMessage;
        public IObservable<Unit> OnConnected => _onConnected;
        public IObservable<DisconnectReason> OnDisconnected => _onDisconnected;
        
        public async UniTask ConnectAsync(ClientConnectOptions opts)
        {
            _networkManager.TransportManager.Transport.SetClientAddress(opts.Host);
            _networkManager.TransportManager.Transport.SetPort(opts.Port);

            _networkManager.ClientManager.OnClientConnectionState += OnState;
            _networkManager.ClientManager.RegisterBroadcast<RawEnvelope>(OnClientReceive);
            _networkManager.ClientManager.StartConnection();
            await UniTask.WaitUntil(() => IsConnected).Timeout(opts.Timeout);
        }

        public UniTask DisconnectAsync(DisconnectReason reason)
        {
            _networkManager.ClientManager.StopConnection();
            _networkManager.ClientManager.UnregisterBroadcast<RawEnvelope>(OnClientReceive);
            _networkManager.ClientManager.OnClientConnectionState -= OnState;
            IsConnected = false;
            return UniTask.CompletedTask;
        }

        public UniTask SendAsync(in Envelope envelope)
        {
            var raw = FishNetEnvelope.ToRaw(envelope);
            var channel = FishNetEnvelope.ChannelFromReliability(envelope.Reliability);
            _networkManager.ClientManager.Broadcast(raw, channel: channel);
            return UniTask.CompletedTask;
        }

        private void OnState(ClientConnectionStateArgs state)
        {
            switch (state.ConnectionState)
            {
                case LocalConnectionState.Started:
                    IsConnected = true;
                    _onConnected.OnNext(Unit.Default);
                    break;
                case LocalConnectionState.Stopped:
                case LocalConnectionState.Stopping:
                    if (IsConnected)
                    {
                        IsConnected = false;
                        _onDisconnected.OnNext(DisconnectReason.ClosedByServer);
                    }

                    break;
            }
        }

        private void OnClientReceive(RawEnvelope raw, Channel channel)
        {
            _onMessage.OnNext(FishNetEnvelope.FromRaw(raw, channel));
        }

        public void Dispose()
        {
            _networkManager.ClientManager.UnregisterBroadcast<RawEnvelope>(OnClientReceive);
            _networkManager.ClientManager.OnClientConnectionState -= OnState;
            if (IsConnected)
                _networkManager.ClientManager.StopConnection();
            _onMessage?.Dispose();
            _onConnected?.Dispose();
            _onDisconnected?.Dispose();
        }
    }
}