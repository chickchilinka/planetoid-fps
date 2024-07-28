using System;
using UniRx;
using Scene.View;
using ViewSystem.Controller;

namespace ApplicationMode.States
{
    public class DelayLoadingState : AbstractGameState
    {
        public override bool StopSequenceOnFail => false;
        public override string LocalizationKey => string.Empty;

        private readonly IViewController _viewController;

        private const float FinishProgressAnimationDuration = 1.5f;
        private const bool ShowLoaderWindow = false;                                        // Right now, this window is not shown
        public DelayLoadingState(IViewController viewController)
        {
            _viewController = viewController;
        }

        public override void Apply()
        {
            if (ShowLoaderWindow)
                _viewController.ShowView<LoaderWindow>();

            Observable.Timer(TimeSpan.FromSeconds(FinishProgressAnimationDuration))
                .Subscribe(_ => OnApplied(true));
        }

        public override void Clear()
        {
            if (ShowLoaderWindow)
                _viewController.HideView<LoaderWindow>();
            base.Clear();
        }
    }
}