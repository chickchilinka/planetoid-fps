using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Base.AssetSystem.Providers
{
    internal sealed class ResourcesAssetProvider : IAssetProvider
    {
        public UniTask<T> Get<T>(string assetId) where T : Object
        { 
            var asset = Resources.Load<T>(assetId);
            return UniTask.FromResult(asset);
        }

        public UniTask Unload<T>(T asset) where T : Object
        {
            if (asset is GameObject)
            {
                return Resources.UnloadUnusedAssets().ToUniTask();
            }

            Resources.UnloadAsset(asset);
            return UniTask.CompletedTask;
        }
    }
}