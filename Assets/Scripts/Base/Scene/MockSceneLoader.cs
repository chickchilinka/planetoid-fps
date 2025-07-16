using Cysharp.Threading.Tasks;
using Scene.Data;
using UniRx;

namespace Scene
{
    public class MockSceneLoader : ISceneLoader
    {
        private ReactiveProperty<SceneLoaderStatus> _status = new();
        public IReadOnlyReactiveProperty<SceneLoaderStatus> Status => _status;
        public string ActiveScene { get; private set; }

        public UniTask LoadSceneAsync(string scene)
        {
            ActiveScene = scene;

            _status.Value = SceneLoaderStatus.Loading;
            _status.Value = SceneLoaderStatus.Loaded;
            return UniTask.CompletedTask;
        }
    }
}