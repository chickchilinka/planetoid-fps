

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Utils.Debugger;
using AddressableAssets = UnityEngine.AddressableAssets;

namespace AddressableAssetsSystem.Services
{
    internal class DownloadAssetsService
    {
        private readonly AssetsModel _assetsModel;

        public DownloadAssetsService(AssetsModel assetsModel)
        {
            _assetsModel = assetsModel;
        }

        public UniTask UpdateCatalog()
        {
            var updateOperation = new UpdateCatalogAsyncOperation();
            if (updateOperation == null) throw new ArgumentNullException(nameof(updateOperation));
            return updateOperation.Task;
        }
        public DownloadAssetsAsyncOperation TryDownloadRemoteAssets(string[] assetNames, Action<bool> onCompleted)
        {
            var references = GetAssetReferences(assetNames);
            return TryDownloadRemoteAssets(references, onCompleted);
        }
        public DownloadAssetsAsyncOperation TryDownloadRemoteAssets(params AddressableAssets.AssetReference[] references)
        {
            return TryDownloadRemoteAssets(references, _ => { });
        }

        private DownloadAssetsAsyncOperation TryDownloadRemoteAssets(IEnumerable<AddressableAssets.AssetReference> references, Action<bool> onCompleted = default)
        {
            var assetReferences = new HashSet<AddressableAssets.AssetReference>(references);
            assetReferences.ExceptWith(_assetsModel.DownloadedAssets);
            return new DownloadAssetsAsyncOperation(assetReferences, onCompleted);
        }

        private AddressableAssets.AssetReference GetAssetReference(string assetName)
        {
            if (!_assetsModel.CatalogInitialized)
                return null;
            
            var assetReference = _assetsModel.Catalog.GetAssetReferenceSettingsByName(assetName)
                ?.AssetReference;

            if (assetReference == null)
                PrintLog.Warn(LogTag.DownloadAssets, $"AssetReference with name {assetName} not found");

            return assetReference;
        }

        private IEnumerable<AddressableAssets.AssetReference> GetAssetReferences(IEnumerable<string> assetNames)
        {
            foreach (var assetName in assetNames)
            {
                var asset = GetAssetReference(assetName);
                if (asset != null)
                    yield return asset;
                else
                {
                    PrintLog.Warn($"Asset with name {assetName} not found!");
                }
            }
        }
    }
}