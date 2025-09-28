using System;
using System.Collections.Generic;
using System.Threading;
using Base.Common.Log;
using Cysharp.Threading.Tasks;
using Scene.Data;
using UniRx;
using UnityEngine.SceneManagement;

namespace Scene
{
    public class SceneLoader :  IDisposable
    {
        public const string ScenePathFormat = "Assets/Scenes/{0}.unity";

        private readonly List<string> _loadingScenes = new();
        private readonly SemaphoreSlim _loadingScenesSemaphore = new(1, 1);
        private static readonly TimeSpan TimeoutDuration = TimeSpan.FromSeconds(15f);
        private readonly ReactiveProperty<SceneLoaderStatus> _status = new(SceneLoaderStatus.None);

        public IReadOnlyReactiveProperty<SceneLoaderStatus> Status => _status;
        public string ActiveScene { get; private set; }

        public SceneLoader()
        {
            ActiveScene = SceneManager.GetActiveScene().name;
        }


        public async UniTask LoadSceneAsync(string scene)
        {
            await _loadingScenesSemaphore.WaitAsync();
            if (_loadingScenes.Contains(scene))
            {
                PrintLog.Error(LogTag.Scene, $"Scene {scene} already being loaded!");
                _loadingScenesSemaphore.Release();
                return;
            }

            using var timeoutLog = Observable
                .Timer(TimeoutDuration)
                .Subscribe(_ => PrintLog.Error(LogTag.Scene, $"Scene {scene} is loading more than 15 seconds!"));

            if (ActiveScene == scene)
            {
                PrintLog.Error(LogTag.Scene, $"Trying loading already loaded scene {scene}");
            }

            _loadingScenes.Add(scene);
            _loadingScenesSemaphore.Release();

            SetStatus(SceneLoaderStatus.Loading);

            await AsyncSceneLoad(scene, LoadSceneMode.Single);

            ActiveScene = scene;

            await _loadingScenesSemaphore.WaitAsync();

            _loadingScenes.Remove(scene);
            if (_loadingScenes.Count > 0)
            {
                PrintLog.Warn(LogTag.Scene,
                    "Not all scenes loaded! Wait for loading Scenes:" + string.Join(", ", _loadingScenes));
                await UniTask.WaitWhile(() => _loadingScenes.Count > 0);
                PrintLog.Info(LogTag.Scene, "End wait for loading Scenes");
            }

            _loadingScenesSemaphore.Release();

            CompleteLoading(scene);
        }

        private async UniTask AsyncSceneLoad(string scene, LoadSceneMode loadSceneMode, float delay = 0f)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delay));
            await SceneManager.LoadSceneAsync(string.Format(ScenePathFormat, scene), loadSceneMode).ToUniTask();
        }

        private void CompleteLoading(string scene)
        {
            PrintLog.Info(LogTag.Scene, $"Scene loaded: {scene}");

            SetStatus(SceneLoaderStatus.Loaded);
        }

        public void Dispose()
        {
            _loadingScenesSemaphore?.Dispose();
        }

        private void SetStatus(SceneLoaderStatus status)
        {
            _status.Value = status;
        }
    }
}