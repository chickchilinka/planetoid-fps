using Rule;
using Scene.Data;
using ViewSystem.Controller;
using General.View;
using Zenject;

namespace Scene.Rules
{
    public class LoadInitialSceneRule : AbstractSignalRule<SceneSignals.LoadInitialScene>
    {
        private readonly IViewController _viewController;
        private readonly ISceneLoader _sceneLoader;
        private readonly SignalBus _signalBus;

        public LoadInitialSceneRule(ISceneLoader sceneLoader,
            SignalBus signalBus,
            IViewController viewController)
        {
            _sceneLoader = sceneLoader;
            _viewController = viewController;
            _signalBus = signalBus;
        }

        protected override void OnSignalFired(SceneSignals.LoadInitialScene signal)
        {
            _signalBus.Subscribe<SceneSignals.LoadingCompleted>(OnLoadingCompleted);
            _sceneLoader.LoadScene(SceneType.Initial);
        }

        private void OnLoadingCompleted(SceneSignals.LoadingCompleted obj)
        {
            _signalBus.TryUnsubscribe<SceneSignals.LoadingCompleted>(OnLoadingCompleted);
            _viewController.ShowView<InitialWindow>();
        }

    }
}