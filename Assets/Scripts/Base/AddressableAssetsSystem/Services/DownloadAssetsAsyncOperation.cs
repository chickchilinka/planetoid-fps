

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Base.Common.Log;
using Base.Common.Utils.Extensions;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using AddressableAssets = UnityEngine.AddressableAssets;

namespace AddressableAssetsSystem.Services
{
    public class DownloadAssetsAsyncOperation
    {
        private const float TimeOut = 15f;
        private const float BytesInMb = 1000000f;

        private readonly UniTask<bool> _task;
        private AsyncOperationHandle _asyncOperationHandle;
        private CancellationTokenSource _cancellationTokenSource;

        public DownloadAssetsAsyncOperation(HashSet<AddressableAssets.AssetReference> assetReferences,
            Action<bool> onCompleted)
        {
            OnCompleted = onCompleted;

            AssetReferences = assetReferences;

            if (assetReferences.IsNullOrEmpty())
            {
                IsOperationFinished = true;
                CompletedOperation(true);
                return;
            }

            _task = TryDownloadRemoteAssets(assetReferences.ToArray());
        }

        public bool IsOperationFinished { get; private set; }
        public Exception Exception { get; private set; }

        public HashSet<AddressableAssets.AssetReference> AssetReferences { get; private set; }


        public bool IsSuccessOperation { get; private set; }

        public FloatReactiveProperty Progress { get; private set; }
        public long Size { get; private set; }

        public event Action<bool> OnCompleted;
        public event Action OnDownloadStarted = delegate { };

        public UniTask<bool> ToUniTask()
        {
            return _task;
        }

        private async UniTask<bool> TryDownloadRemoteAssets(IReadOnlyList<AddressableAssets.AssetReference> assetReferences)
        {
            var actualAssets = await GetAssetsWithNonZeroSize(assetReferences);

            var isSuccess = true;

            if (actualAssets.Count > 0)
            {
                var sleepTimeout = Screen.sleepTimeout;
                Screen.sleepTimeout = SleepTimeout.NeverSleep;

                isSuccess = await DownloadAssets(actualAssets);

                Screen.sleepTimeout = sleepTimeout;
            }

            IsOperationFinished = true;

            CompletedOperation(isSuccess);

            return isSuccess;
        }

        private async UniTask<bool> DownloadAssets(IReadOnlyList<AddressableAssets.AssetReference> assetReferences)
        {
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _asyncOperationHandle =
                    AddressableAssets.Addressables.GetDownloadSizeAsync(assetReferences.AsEnumerable());

                await _asyncOperationHandle.ToUniTask(cancellationToken: _cancellationTokenSource.Token);
                await UniTask.Yield();

                if (_asyncOperationHandle.Status == AsyncOperationStatus.Failed)
                {
                    ErrorOperation(_asyncOperationHandle.OperationException);
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorOperation(ex);
                return false;
            }

            await UniTask.Yield();

            var size = (long)_asyncOperationHandle.Result;

            if (assetReferences.Count == 0)
                return true;

            PrintLog.Info(LogTag.Addressable, "Loading started for assets " + assetReferences.Log());

            try
            {
                AddressableAssets.Addressables.Release(_asyncOperationHandle);
                _asyncOperationHandle = AddressableAssets.Addressables.DownloadDependenciesAsync(
                    assetReferences.AsEnumerable(),
                    AddressableAssets.Addressables.MergeMode.Union);

                Size = size;

                UpdateDownloadProgress(_asyncOperationHandle);

                await _asyncOperationHandle.ToUniTask(cancellationToken: _cancellationTokenSource.Token);
                await UniTask.Yield();

                if (_asyncOperationHandle.Status == AsyncOperationStatus.Failed)
                {
                    ErrorOperation(_asyncOperationHandle.OperationException);
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorOperation(ex);
                return false;
            }

            var isSuccess = _asyncOperationHandle.IsValid() &&
                            _asyncOperationHandle.Status == AsyncOperationStatus.Succeeded;

            PrintLog.Info(LogTag.Addressable,
                $"download assets completed with {(isSuccess ? "SUCCESS" : "errorMessage")}\n" +
                $"{(isSuccess ? assetReferences.Log() : string.Empty)}");

            return isSuccess;
        }

        private async UniTask<IReadOnlyList<AddressableAssets.AssetReference>> GetAssetsWithNonZeroSize(
            IReadOnlyList<AddressableAssets.AssetReference> assetReferences)
        {
            var tasks = new UniTask<long>[assetReferences.Count];

            for (var i = 0; i < assetReferences.Count; i++)
                tasks[i] = GetRemoteAssetSize(assetReferences[i]);

            var sizes = await UniTask.WhenAll(tasks);

            var packedSizes = new List<AddressableAssets.AssetReference>();
            for (var i = 0; i < assetReferences.Count; i++)
            {
                var assetSize = sizes[i];
                var assetReference = assetReferences[i];

                var needToDownload = assetSize > 0;
                if (needToDownload)
                {
                    PrintLog.Info(LogTag.DownloadAssets,
                        $"Need to download «<color=white>{assetReference.RuntimeKey}</color>», size: {assetSize / BytesInMb:F2} MB");
                    packedSizes.Add(assetReference);
                }
            }

            return packedSizes;
        }

        private async void UpdateDownloadProgress(AsyncOperationHandle downloadAsync)
        {
            Progress = new FloatReactiveProperty();
            OnDownloadStarted?.Invoke();

            var noProgressTime = 0f;

            while (!downloadAsync.IsDone && !_cancellationTokenSource.IsCancellationRequested)
            {
                var status = downloadAsync.GetDownloadStatus();

                if (Math.Abs(Progress.Value - status.Percent) > 0.001f)
                {
                    Progress.Value = status.Percent;
                    noProgressTime = 0;
                }
                else
                {
                    noProgressTime += Time.deltaTime;

                    if (noProgressTime > TimeOut) ErrorOperation(new Exception("TimeOut"));
                }

                await UniTask.Yield();
            }

            Progress?.Dispose();
            Progress = null;
        }

        private UniTask<long> GetRemoteAssetSize(AddressableAssets.AssetReference assetReference)
        {
            if (assetReference == null)
                return UniTask.FromResult<long>(0);
            return AddressableAssets.Addressables.GetDownloadSizeAsync(assetReference).ToUniTask();
        }

        private void ErrorOperation(Exception exception)
        {
            PrintLog.Error(LogTag.DownloadAssets, "-> " + exception.Message + "\n" + exception);

            Exception = exception;
            CompletedOperation(false);
        }

        private void CompletedOperation(bool success)
        {
            _cancellationTokenSource?.Cancel();

            if (!success)
                AddressableAssets.Addressables.Release(_asyncOperationHandle);

            IsSuccessOperation = success;
            OnCompleted?.Invoke(success);
        }
    }
}