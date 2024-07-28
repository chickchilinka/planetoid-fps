using Scene;
using Scene.Data;
using Zenject;

namespace ApplicationMode.States
{
    public class LoadMapScene : AbstractGameState
    {
        private readonly ISceneLoader _sceneLoader;
        private readonly SignalBus _signalBus;
        
        public override bool StopSequenceOnFail => false;
        public override string LocalizationKey => "mapLoad";

        public LoadMapScene(ISceneLoader sceneLoader,
            SignalBus signalBus)
        {
            _sceneLoader = sceneLoader;
            _signalBus = signalBus;
        }

        public override void Apply()
        {
            _signalBus.Subscribe<SceneSignals.LoadingCompleted>(OnLoadingCompleted);
            _sceneLoader.LoadScene(SceneType.Map);
        }

        private void OnLoadingCompleted()
        {
            _signalBus.TryUnsubscribe<SceneSignals.LoadingCompleted>(OnLoadingCompleted);
            OnApplied(true);
        }
    }
}