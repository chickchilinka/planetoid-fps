using Scene.Data;

namespace Scene
{
    public interface ISceneLoader
    {
        SceneType ActiveScene { get; }
        void LoadScene(SceneType sceneType);
        void LoadAdditionalScene(string scene = null, bool sceneIsActive = false);
        void UnloadAdditionalScene(string scene);
        bool IsSceneLoaded(string scene);
    }
}