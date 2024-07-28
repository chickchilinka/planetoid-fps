using Scene.Data;

namespace Scene
{
    public class SceneSignals
    {
        public class LoadingRequested
        {
            public SceneType SceneType { get; }

            public LoadingRequested(SceneType sceneType) 
            {
                SceneType = sceneType;
            }
        }

        public class LoadingStarted
        {
            public SceneType ActiveScene { get; }

            public LoadingStarted(SceneType activeScene)
            {
                ActiveScene = activeScene;
            }
        }

        public class UnloadingStarted
        {
            public string Scene { get; }

            public UnloadingStarted(string scene)
            {
                Scene = scene;
            }
        }

        public class LoadingCompleted
        {
            public SceneType SceneType { get; }

            public LoadingCompleted(SceneType sceneType)
            {
                SceneType = sceneType;
            }
        }

        public class AdditionalSceneLoadingStarted
        {
            public string SceneName { get; }

            public AdditionalSceneLoadingStarted(string sceneName)
            {
                SceneName = sceneName;
            }
        }

        public class AdditionalSceneLoadingCompleted
        {
            public string SceneName { get; }

            public AdditionalSceneLoadingCompleted(string sceneName)
            {
                SceneName = sceneName;
            }
        }

        public class LoadInitialScene
        {
        }
    }
}