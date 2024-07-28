using Scene.Data;
using Zenject;

namespace Scene
{
    public class EditorSceneLoader : ISceneLoader
    {
        private readonly SignalBus _signalBus;

        public SceneType ActiveScene { get; private set; }

        public EditorSceneLoader(SignalBus signalBus)
        {
            _signalBus = signalBus;
        }

        public void LoadScene(SceneType sceneType)
        {
            ActiveScene = sceneType;

            _signalBus.Fire(new SceneSignals.LoadingCompleted(sceneType));
        }
        
        public void LoadAdditionalScene(string scene = null, bool sceneIsActive = false)
        {
            
        }

        public void UnloadAdditionalScene(string scene)
        {
            
        }

        public bool IsSceneLoaded(string scene)
        {
            return false;
        }
    }
}