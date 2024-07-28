using System;
using System.Collections.Generic;
using System.Threading;
using AddressableAssetsSystem.Services;
using Cysharp.Threading.Tasks;
using Scene.Data;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils.Debugger;
using Utils.Extensions;
using Zenject;

namespace Scene
{
    public class SceneLoader : ISceneLoader, IDisposable
    {
        public const string ScenePathFormat = "Assets/Scenes/{0}.unity";

        private readonly SignalBus _signalBus;
        private readonly AddressableAssetsService _addressableAssetsService;

        private readonly List<string> _loadingScenes = new();
        private readonly SemaphoreSlim _loadingScenesSemaphore = new(1, 1);
        private static readonly TimeSpan TimeoutDuration = TimeSpan.FromSeconds(15f);

        public SceneType ActiveScene { get; private set; }

        public SceneLoader(SignalBus signalBus, 
            AddressableAssetsService addressableAssetsService)
        {
            _signalBus = signalBus;
            _addressableAssetsService = addressableAssetsService;

            ActiveScene = SceneManager.GetActiveScene().name.ToEnum<SceneType>();
        }


        public async void LoadScene(SceneType sceneType)
        {
            var scene = sceneType.ToString();

            await _loadingScenesSemaphore.WaitAsync();
            if (_loadingScenes.Contains(scene))
            {
                PrintLog.Error(LogTag.Scene, $"Scene {scene} already being loaded!");
                _loadingScenesSemaphore.Release();
                return;
            }
            
            using var timeoutLog = Observable
                .Timer(TimeoutDuration)
                .Subscribe(_=> PrintLog.Error(LogTag.Scene, $"Scene {scene} is loading more than 15 seconds!"));

            if (ActiveScene == sceneType)
            {
                PrintLog.Info(LogTag.Scene, $"Start unload {scene} scene for reload");
                _signalBus.Fire(new SceneSignals.UnloadingStarted(scene));
            }
            
            _loadingScenes.Add(scene);
            _loadingScenesSemaphore.Release();

            _signalBus.Fire(new SceneSignals.LoadingRequested(sceneType));
            _signalBus.Fire(new SceneSignals.LoadingStarted(ActiveScene));

            if (!_addressableAssetsService.IsAllNecessaryAssetsLoaded)
            {
                PrintLog.Info(LogTag.Scene, "Start wait for necessary assets");
                await UniTask.WaitUntil(() => _addressableAssetsService.IsAllNecessaryAssetsLoaded);
                await UniTask.Yield();
                PrintLog.Info(LogTag.Scene, "End wait for necessary assets");
            }
            await AsyncSceneLoad(sceneType.ToString(), LoadSceneMode.Single);

            ActiveScene = sceneType;

            await _loadingScenesSemaphore.WaitAsync();
            
            _loadingScenes.Remove(scene);
            if (_loadingScenes.Count > 0)
            {
                PrintLog.Warn(LogTag.Scene, "Not all scenes loaded! Wait for loading Scenes:" + string.Join(", ", _loadingScenes));
                await UniTask.WaitWhile(() => _loadingScenes.Count > 0);
                PrintLog.Info(LogTag.Scene, "End wait for loading Scenes");
            }
            _loadingScenesSemaphore.Release();

            Resources.UnloadUnusedAssets();
            GC.Collect();

            CompleteLoading(sceneType);
        }

        public async void LoadAdditionalScene(string scene, bool sceneIsActive = false)
        {
            if (_loadingScenes.Contains(scene) || string.IsNullOrEmpty(scene))
                return;

            _loadingScenes.Add(scene);

            _signalBus.Fire(new SceneSignals.AdditionalSceneLoadingStarted(scene));

            await AsyncSceneLoad(scene, LoadSceneMode.Additive);

            _signalBus.Fire(new SceneSignals.AdditionalSceneLoadingCompleted(scene));

            if (sceneIsActive)
                SceneManager.SetActiveScene(SceneManager.GetSceneByName(scene));

            _loadingScenes.Remove(scene);
        }

        private async UniTask AsyncSceneLoad(string scene, LoadSceneMode loadSceneMode, float delay = 0f)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delay));
            await SceneManager.LoadSceneAsync(string.Format(ScenePathFormat, scene), loadSceneMode).ToUniTask();
        }

        public async void UnloadAdditionalScene(string scene)
        {
            if (_loadingScenes.Contains(scene))
                return;

            var scenePath = string.Format(ScenePathFormat, scene);
            if (SceneManager.GetSceneByName(scenePath).IsValid())
            {
                _signalBus.Fire(new SceneSignals.UnloadingStarted(scene));

                _loadingScenes.Add(scene);

                await SceneManager.UnloadSceneAsync(scenePath).ToUniTask();

                Resources.UnloadUnusedAssets();
                GC.Collect();
            }

            _loadingScenes.Remove(scene);
        }

        public bool IsSceneLoaded(string scene)
        {
            return SceneManager.GetSceneByName(scene).isLoaded;
        }

        private void CompleteLoading(SceneType sceneType)
        {
            PrintLog.Info(LogTag.Scene, $"Scene loaded {sceneType}");
            _signalBus.Fire(new SceneSignals.LoadingCompleted(sceneType));
        }

        public void Dispose()
        {
            _loadingScenesSemaphore?.Dispose();
        }
    }
}