using Cysharp.Threading.Tasks;
using Scene;
using Zenject;

namespace ApplicationMode.States
{
    public class LoadMapSceneState : AbstractGameState
    {
        private readonly ISceneLoader _sceneLoader;
        private readonly SignalBus _signalBus;
        
        public override bool StopSequenceOnFail => false;
        public override string LocalizationKey => "mapLoad";

        public LoadMapSceneState(ISceneLoader sceneLoader,
            SignalBus signalBus)
        {
            _sceneLoader = sceneLoader;
            _signalBus = signalBus;
        }

        public override async UniTaskVoid Apply()
        {
            await _sceneLoader.LoadSceneAsync("Menu");
            OnApplied(true);
        }
    }
}