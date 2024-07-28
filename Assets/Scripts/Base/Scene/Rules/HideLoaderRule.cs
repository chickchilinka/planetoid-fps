using Rule;
using Scene.View;
using ViewSystem.Controller;

namespace Scene.Rules
{
    public class HideLoaderRule : AbstractSignalRule<SceneSignals.LoadingCompleted>
    {
        private readonly IViewController _viewController;

        public HideLoaderRule(IViewController viewController)
        {
            _viewController = viewController;
        }

        protected override void OnSignalFired(SceneSignals.LoadingCompleted signal)
        {
            _viewController.HideView<TransitionWindow>();
        }
    }
}