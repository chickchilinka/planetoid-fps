// MarrowMachine CONFIDENTIAL
// __________________
//
// [2016] - [2024] MarrowMachine LLC
// All Rights Reserved.
//
// NOTICE:  All information contained herein is, and remains
// the property of MarrowMachine LLC and its suppliers,
// if any.  The intellectual and technical concepts contained
// herein are proprietary to MarrowMachine LLC
// and its suppliers and may be covered by U.S. and Foreign Patents,
// patents in process, and are protected by trade secret or copyright law.
// Dissemination of this information or reproduction of this material
// is strictly forbidden unless prior written permission is obtained
// from MarrowMachine LLC.

using System;
using System.Collections.Generic;
using System.Linq;
using AddressableAssetsSystem.Data;
using UniRx;
using Utils.Debugger;
using Utils.Extensions;
using AddressableAssets = UnityEngine.AddressableAssets;

namespace AddressableAssetsSystem.Services
{
    internal class AssetsModel : IDisposable
    {
        public readonly HashSet<AddressableAssets.AssetReference> DownloadedAssets = new();
        public bool CatalogInitialized
        {
            get
            {
                if (Catalog == null)
                {
                    PrintLog.Warn(LogTag.Addressable, "Catalog wasn't initialized");
                    return false;
                }
                return true;
            }
        }

        public AssetPacksCatalogRegistry Catalog { get; private set; }

        private readonly List<DownloadAssetsAsyncOperation> _downloadAssetsAsyncOperations = new();
        public IObservable<Unit> DownloadStarted => _downloadStartCommand;
        public bool IsAnyDownload => !_downloadAssetsAsyncOperations.IsNullOrEmpty();

        public ReactiveProperty<float> Progress { get; } = new();
        private readonly ReactiveCommand _downloadStartCommand = new();

        private IDisposable _updateProgressStream;

        public long Size => _downloadAssetsAsyncOperations.IsNullOrEmpty() ? 0 : _downloadAssetsAsyncOperations.Sum(operation => operation.Size);

        internal void InitializeCatalog(AssetPacksCatalogRegistry catalog)
        {
            Catalog = catalog;
            PrintLog.Info(LogTag.Addressable, "Catalog initialized!");
        }
        public void AddDownloadAssetsAsyncOperation(DownloadAssetsAsyncOperation operation)
        {
            _downloadAssetsAsyncOperations.Add(operation);
            operation.OnDownloadStarted += OnDownloadStarted;
        }

        private void OnDownloadStarted()
        {
            _downloadStartCommand.Execute();
            UpdateProgress();
        }

        private void UpdateProgress()
        {
            _updateProgressStream?.Dispose();
            
            if (_downloadAssetsAsyncOperations.Count == 0)
                return;

            var totalSize = _downloadAssetsAsyncOperations.Select(x => x.Size).Sum();
            
            _updateProgressStream = Observable.EveryUpdate().Subscribe(_ =>
            {
                var progress = 0f;
                foreach (var operation in _downloadAssetsAsyncOperations)
                {
                    if (operation.Progress != null) 
                        progress += operation.Progress.Value * operation.Size / totalSize;
                }
                Progress.Value = progress;
            });
        }

        public void RemoveDownloadAssetsAsyncOperation(DownloadAssetsAsyncOperation operation)
        {
            operation.OnDownloadStarted -= OnDownloadStarted;
            _downloadAssetsAsyncOperations.Remove(operation);
            
            UpdateProgress();
        }

        public void AddToDownloadedAssets(IEnumerable<AddressableAssets.AssetReference> assetReferences)
        {
            foreach (var assetReference in assetReferences)
            {
                if (!DownloadedAssets.Add(assetReference))
                    return;
            }
        }

        public void Dispose()
        {
            _downloadStartCommand?.Dispose();
            _updateProgressStream?.Dispose();
            Progress?.Dispose();
        }
    }
}