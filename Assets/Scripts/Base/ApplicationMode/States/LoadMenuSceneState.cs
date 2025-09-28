using Cysharp.Threading.Tasks;
using Scene;

namespace ApplicationMode.States
{
    public class LoadMenuSceneState : AbstractGameState
    {
        private readonly ISceneLoader _sceneLoader;

        public override bool StopSequenceOnFail => false;
        public override string LocalizationKey => "mapLoad";

        public LoadMenuSceneState(ISceneLoader sceneLoader)
        {
            _sceneLoader = sceneLoader;
        }

        public override async UniTaskVoid Apply()
        {
            await _sceneLoader.LoadSceneAsync("Menu");
            OnApplied(true);
        }
    }
}