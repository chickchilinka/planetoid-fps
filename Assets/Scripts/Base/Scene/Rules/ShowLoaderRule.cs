using Rule;
using Scene.Data;
using Scene.View;
using ViewSystem.Controller;

namespace Scene.Rules
{
    public class ShowLoaderRule : AbstractSignalRule<SceneSignals.LoadingStarted>
    {
        private readonly IViewController _viewController;

        private const SceneType SceneWithSimpleTransition = SceneType.Initial;

        public ShowLoaderRule(IViewController viewController)
        {
            _viewController = viewController;
        }

        protected override void OnSignalFired(SceneSignals.LoadingStarted signal)
        {
            var isAnimatedTransition = signal.ActiveScene != SceneWithSimpleTransition;

            _viewController.ShowView<TransitionWindow, TransitionWindow.Data>(new TransitionWindow.Data() { IsAnimatedTransition = isAnimatedTransition });
        }
    }
}