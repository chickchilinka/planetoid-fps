using General.View;
using Stability;
using Stability.Models;
using ViewSystem;
using ViewSystem.Controller;
using Zenject;

namespace ApplicationMode.States
{
    public class ShowInitialWindowState : AbstractGameState 
    {
        private readonly IViewController _viewController;
        private readonly SignalBus _signalBus;
        
        public override bool StopSequenceOnFail => false;
        public override string LocalizationKey => string.Empty;

        public ShowInitialWindowState(IViewController viewController, SignalBus signalBus)
        {
            _viewController = viewController;
            _signalBus = signalBus;
        }
        
        public override void Apply()
        {
            _signalBus.Subscribe<ViewSignals.Shown>(OnViewShown);
            _viewController.ShowView<InitialWindow>();
        }

        private void OnViewShown(ViewSignals.Shown signal)
        {
            var success = signal.IsEqual<InitialWindow>();
            if (!success)
            {
                var errorModel = new ErrorData("ShowInitialWindowState Failed","Error during initialization start window",!StopSequenceOnFail);
                _signalBus.Fire(new StabilitySignals.HandleError(errorModel));
            }
            OnApplied(success);
            _signalBus.TryUnsubscribe<ViewSignals.Shown>(OnViewShown);
        }
    }
}