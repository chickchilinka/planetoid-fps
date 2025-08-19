namespace Base.Network.Service
{
    public class NetworkService
    {
        public bool IsServer { get; }

        public NetworkService(bool isServer)
        {
            IsServer = isServer;
        }
        
    }
}