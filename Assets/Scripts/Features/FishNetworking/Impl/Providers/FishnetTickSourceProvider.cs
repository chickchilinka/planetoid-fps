using Base.Network.Provider;
using FishNet.Managing.Timing;

namespace Features.FishNetworking.Impl.Providers
{
    public class FishnetTickSourceProvider: ITickSource
    {
        private readonly TimeManager _timeManager;

        public FishnetTickSourceProvider(TimeManager timeManager)
        {
            _timeManager = timeManager;
        }

        public uint ServerTick => _timeManager.Tick;
        public uint ClientTick => _timeManager.LocalTick;
    }
}