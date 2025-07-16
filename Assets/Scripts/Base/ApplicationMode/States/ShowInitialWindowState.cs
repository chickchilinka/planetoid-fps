using Base.ApplicationMode.View;
using Cysharp.Threading.Tasks;
using ViewSystem;
using ViewSystem.Controller;
using Zenject;

namespace ApplicationMode.States
{
    public class ShowInitialWindowState : AbstractGameState 
    {
        private readonly IViewService _viewService;
        private readonly SignalBus _signalBus;
        
        public override bool StopSequenceOnFail => false;
        public override string LocalizationKey => string.Empty;

        public ShowInitialWindowState(IViewService viewService, SignalBus signalBus)
        {
            _viewService = viewService;
            _signalBus = signalBus;
        }
        
        public override UniTaskVoid Apply()
        {
            _signalBus.Subscribe<ViewSignals.Shown>(OnViewShown);
            _viewService.ShowView<InitialWindow>();
            return new UniTaskVoid();
        }

        private void OnViewShown(ViewSignals.Shown signal)
        {
            var success = signal.IsEqual<InitialWindow>();
            OnApplied(success);
            _signalBus.TryUnsubscribe<ViewSignals.Shown>(OnViewShown);
        }
    }
}