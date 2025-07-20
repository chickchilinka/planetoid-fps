using System;
using System.Collections.Generic;
using System.Linq;
using AddressableAssetsSystem.Data;
using AddressableAssetsSystem.Registry;
using Base.Common.Log;
using Cysharp.Threading.Tasks;
using UniRx;
using ViewSystem.Controller;
using AddressableAssets = UnityEngine.AddressableAssets;

namespace AddressableAssetsSystem.Services
{
    public class AddressableAssetsService
    {
        private readonly AssetPacksCatalogRegistryReference _packsCatalogReference;
        private readonly AssetPacksCatalogRegistry _catalogFallback;
        
        private readonly AssetsService _assetsService;
        private readonly DownloadAssetsService _downloadAssetsService;
        private readonly AssetsModel _assetsModel;
        private readonly IViewService _viewService;

        public bool IsAllNecessaryAssetsLoaded => _assetsService.IsAllNecessaryAssetsLoaded;
        public long DownloadAssetsSize => _assetsModel.Size;
        public bool IsDownloadingAnyAsset => _assetsModel.IsAnyDownload;
        public IReadOnlyReactiveProperty<float> DownloadProgress => _assetsModel.Progress;
        public IObservable<Unit> DownloadStarted => _assetsModel.DownloadStarted;

        internal AddressableAssetsService(AssetPacksCatalogRegistryReference packsCatalogReference,
            AssetPacksCatalogRegistry catalogFallback,
            AssetsService assetsService,
            DownloadAssetsService downloadAssetsService,
            AssetsModel assetsModel,
            IViewService viewService)
        {
            _packsCatalogReference = packsCatalogReference;
            _catalogFallback = catalogFallback;
            _assetsService = assetsService;
            _downloadAssetsService = downloadAssetsService;
            _assetsModel = assetsModel;
            _viewService = viewService;
        }
        
        public async UniTask Initialize()
        {
            await AddressableAssets.Addressables.InitializeAsync().ToUniTask();
        }
        public async UniTask UpdateCatalog()
        {
            await _downloadAssetsService.UpdateCatalog();
            await InitializeCatalog();
        }

        public UniTask CacheLocalAssets()
        {
            var localAssets = _assetsService.GetLocalAssets();
            return _assetsService.CacheAssetsAsync(localAssets);
        }

        private async UniTask InitializeCatalog()
        {
            var remoteCatalog = await LoadRemoteCatalogAsync();
            _assetsModel.InitializeCatalog(remoteCatalog ? remoteCatalog : _catalogFallback);
        }

        private async UniTask<AssetPacksCatalogRegistry> LoadRemoteCatalogAsync()
        {
            try
            {
                if (await _downloadAssetsService.TryDownloadRemoteAssets(_packsCatalogReference).ToUniTask())
                {
                    var registry = await _assetsService.LoadValidAssetAsync<AssetPacksCatalogRegistry>(_packsCatalogReference);
                    if (!registry) 
                        PrintLog.Error(LogTag.DownloadAssets, "Failed to load remote catalog. Use fallback.");
                    return registry;
                }
                PrintLog.Error(LogTag.DownloadAssets, "Failed to download remote catalog. Use fallback.");
                return null;
            }
            catch (Exception e)
            {
                PrintLog.Error(LogTag.DownloadAssets, "Failed to download remote catalog: " + e.Message, " Use fallback.");
                return null;
            }
        }

        public UniTask CacheAssetsAsync(params string[] assetNames)
        {
            return _assetsService.CacheAssetsAsync(assetNames);
        }

        public T GetCachedAsset<T>(string assetName) where T : class
        {
            return _assetsService.GetCachedAsset<T>(assetName);
        }

        public bool CanGetCachedAsset(string assetName)
        {
            return _assetsService.CanGetCachedAsset(assetName);
        }

        public void UnloadAssetsByLabel(string label)
        {
            _assetsService.UnloadAssetsByLabel(label);
        }

        public IEnumerable<string> GetAllAddressableAssets()
        {
            return _assetsService.GetAllAddressableAssets();
        }

        public IEnumerable<string> GetActualAssets()
        {
            return _assetsService.GetActualAssets();
        }


        public void TryDownloadActualAssets()
        {
            var assetNames = _assetsService.GetActualAssets().ToArray();
            TryDownloadRemoteAssets(assetNames, result => DownloadCompleted(result, assetNames));
        }
        public void TryDownloadRemoteAssets(string[] assetNames, Action<bool> onCompleted = null)
        {
            var downloadAssetsAsyncOperation =
                _downloadAssetsService.TryDownloadRemoteAssets(assetNames, onCompleted);

            if (downloadAssetsAsyncOperation.IsOperationFinished)
            {
                return;
            }
            
            downloadAssetsAsyncOperation.OnCompleted += _ => DownloadCompleted(downloadAssetsAsyncOperation);

            _assetsModel.AddDownloadAssetsAsyncOperation(downloadAssetsAsyncOperation);
        }
        private void DownloadCompleted(bool isSuccess, IReadOnlyList<string> assetNames)
        {
            if (!isSuccess)
            {
                ContinueWithError(TryDownloadActualAssets);
                return;
            }
            _assetsService.CacheAssetsAsync(assetNames).Forget();
        }
        private void DownloadCompleted(DownloadAssetsAsyncOperation downloadAssetsAsyncOperation)
        {
            // if(downloadAssetsAsyncOperation.IsSuccessOperation)
            //     _viewController.HideView<InfoPopUp>();
            //
            if(downloadAssetsAsyncOperation.IsSuccessOperation)
                _assetsModel.AddToDownloadedAssets(downloadAssetsAsyncOperation.AssetReferences);
            
            _assetsModel.RemoveDownloadAssetsAsyncOperation(downloadAssetsAsyncOperation);
        }

        public void ContinueWithError(Action callback)
        {
            // var data = new InfoPopUpData("Download Assets ", "Сheck your internet connection and try again")
            //     .SetIgnoreLocalization().SetHideAction(callback);
            //
            // _viewController.ShowView<InfoPopUp, InfoPopUpData>(data);
        }
    }
}