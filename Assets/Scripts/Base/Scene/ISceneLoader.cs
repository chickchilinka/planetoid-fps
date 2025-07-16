using System;
using Cysharp.Threading.Tasks;
using Scene.Data;
using UniRx;

namespace Scene
{
    public interface ISceneLoader
    {
        IReadOnlyReactiveProperty<SceneLoaderStatus> Status { get; }
        string ActiveScene { get; }
        UniTask LoadSceneAsync(string sceneType);
    }
}