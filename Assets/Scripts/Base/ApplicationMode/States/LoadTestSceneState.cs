using Cysharp.Threading.Tasks;
using Scene;
using Zenject;

namespace ApplicationMode.States
{
    public class LoadTestSceneState: AbstractGameState
    {
        private readonly ISceneLoader _sceneLoader;
        
        public override bool StopSequenceOnFail => false;
        public override string LocalizationKey => "mapLoad";

        public LoadTestSceneState(ISceneLoader sceneLoader)
        {
            _sceneLoader = sceneLoader;
        }

        public override async UniTaskVoid Apply()
        {
            await _sceneLoader.LoadSceneAsync("Playground");
            OnApplied(true);
        }
    }
}