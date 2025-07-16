using Base.ApplicationMode;
using Cysharp.Threading.Tasks;
using General;
using Zenject;

namespace ApplicationMode.States
{
    public class StartSessionState : AbstractGameState
    {
        private readonly SignalBus _signalBus;
        
        public override bool StopSequenceOnFail => false;
        public override string LocalizationKey => "moreLoading";

        public StartSessionState(SignalBus signalBus)
        {
            _signalBus = signalBus;
        }
        
        public override UniTaskVoid Apply()
        {
            _signalBus.Fire(new GeneralAppSignals.SessionStarted());

            OnApplied(true);
            return new UniTaskVoid();
        }
    }
}