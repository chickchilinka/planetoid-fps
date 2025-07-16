

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AddressableAssetsSystem.Data;
using AddressableAssetsSystem.Handler;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Utils.Extensions;
using Zenject;
using AddressableAssets = UnityEngine.AddressableAssets;

namespace AddressableAssetsSystem.Services
{
    internal class AssetsService : IInitializable, IDisposable
    {
        private readonly List<IAddressableAssetsHandler> _handlers;
        private readonly AssetsModel _assetsModel;

        public AssetsService(AssetsModel assetsModel,
            List<IAddressableAssetsHandler> handlers)
        {
            _assetsModel = assetsModel;
            _handlers = handlers;
        }

        public async UniTask CacheAssetsAsync(IEnumerable<string> assetNames)
        {
            if (assetNames == null || !_assetsModel.CatalogInitialized)
                return;

            var tasks = new List<(string name, UniTask<object> asset)>();

            foreach (var assetName in assetNames)
            {
                var settings = _assetsModel.Catalog.GetAssetReferenceSettingsByName(assetName);
                if (settings?.AssetReference != null && (!settings.AssetReference.IsValid() || !settings.AssetReference.IsDone))
                {
                    var task = LoadValidAssetAsync<object>(settings.AssetReference);
                    tasks.Add((settings.Name, task));
                }
            }

            if (tasks.IsNullOrEmpty())
                return;
            
            var assets = await UniTask.WhenAll(tasks.Select(tuple => tuple.asset));

            if (!tasks.IsNullOrEmpty())
            {
                var stringBuilder = new StringBuilder("[AssetService] Cashed assets: ");
                stringBuilder.AppendJoin(", ", tasks.Select(task => task.name));
                Debug.Log(stringBuilder.ToString());
            }
        }
        public bool CanGetCachedAsset(string assetName)
        {
            return _assetsModel.CatalogInitialized && _assetsModel.Catalog.HasAssetReferenceSettingsWithName(assetName);
        }
        
        public T GetCachedAsset<T>(string assetName) where T : class
        {
            if (!_assetsModel.CatalogInitialized)
                return null;
            var settings = _assetsModel.Catalog.GetAssetReferenceSettingsByName(assetName);

            if (settings != null && settings.AssetReference.IsValid() && settings.AssetReference.IsDone)
            {
                return settings.AssetReference.OperationHandle.Result as T;
            }

            Debug.LogWarning($"[AssetService] Asset with name {assetName} not found");

            return null;
        }

        public void UnloadAssetsByLabel(string label)
        {
            if (!_assetsModel.CatalogInitialized)
                return;
            var settingses = _assetsModel.Catalog.GetSettingsesByLabel(label);

            if (!settingses.IsNullOrEmpty())
                UnloadCachedAssets(settingses.Select(x => x.Name).ToArray());
        }

        private void UnloadCachedAssets(IEnumerable<string> assets)
        {
            if (assets == null || !_assetsModel.CatalogInitialized)
                return;

            foreach (var asset in assets)
            {
                var setting = _assetsModel.Catalog.GetAssetReferenceSettingsByName(asset);
                setting?.AssetReference.ReleaseAsset();
            }
        }

        internal UniTask<T> LoadValidAssetAsync<T>(AddressableAssets.AssetReference assetReference) where T : class
        {
            if (assetReference.IsValid() && assetReference.OperationHandle.IsValid())
            {
                AddressableAssets.Addressables.ResourceManager.Acquire(assetReference.OperationHandle);
                return assetReference.OperationHandle.Convert<T>().ToUniTask();
            }
            return assetReference.LoadAssetAsync<T>().ToUniTask();
        }

        public IEnumerable<string> GetAllAddressableAssets()
        {
            if (_assetsModel.CatalogInitialized)
                return _assetsModel.Catalog.GetAllAssets().Select(x => x.Name).ToList();
            return Enumerable.Empty<string>();
        }

        public IEnumerable<string> GetLocalAssets()
        {
            if (!_assetsModel.CatalogInitialized)
                yield break;
            var assetSettings = _assetsModel.Catalog.GetSettingsesByLabel(AssetLabelConst.LocalAssetPack);
            foreach (var setting in assetSettings)
                yield return setting.Id;
        }
        
        public IEnumerable<string> GetActualAssets()
        {
            foreach (var addressableAssetsHandler in _handlers)
            foreach (var handlerAsset in addressableAssetsHandler.GetActualAssets())
                    yield return handlerAsset;
        }

        public bool IsAllNecessaryAssetsLoaded
        {
            get
            {
                foreach (var handler in _handlers)
                {
                    if (handler.IsMustBeLoaded && !IsAssetsByPackCached(handler.Label))
                        return false;
                }
                return true;
            }
        }

        private bool IsAssetsByPackCached(string label)
        {
            if (!_assetsModel.CatalogInitialized)
                return false;
            var settingses = _assetsModel.Catalog.GetSettingsesByLabel(label);

            foreach (var settings in settingses)
            {
                if (settings == null || !settings.AssetReference.IsValid() || !settings.AssetReference.IsDone)
                {
                    //Debug.Log("[AssetService] waiting asset: " + settings.Name);
                    return false;
                }
            }
            return true;
        }

        public void Initialize()
        {
            foreach (var handler in _handlers) 
                handler.Initialize();
        }

        public void Dispose()
        {
            foreach (var handler in _handlers) 
                handler.Dispose();
        }
    }
}