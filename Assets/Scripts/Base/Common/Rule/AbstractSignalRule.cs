using Zenject;

namespace Base.Common.Rule
{
    public abstract class AbstractSignalRule<TSignal> : AbstractRule
    {
        protected SignalBus SignalBus { get; private set; }
        
        [Inject]
        private void Construct(SignalBus signalBus)
        {
            SignalBus = signalBus;
        }
        
        public override void Initialize()
        {
            SignalBus.Subscribe<TSignal>(OnSignalFired);
        }
        
        public override void Dispose()
        {
            SignalBus.TryUnsubscribe<TSignal>(OnSignalFired);
        }
        
        protected abstract void OnSignalFired(TSignal signal);
    }
}
