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
using Cysharp.Threading.Tasks;
using UnityEngine.ResourceManagement.AsyncOperations;
using Utils.Debugger;
using Utils.Extensions;
using AddressableAssets = UnityEngine.AddressableAssets;

namespace AddressableAssetsSystem.Services
{
    public class UpdateCatalogAsyncOperation
    {
        public UniTask Task { get; private set; }

        public UpdateCatalogAsyncOperation(Action<Exception> onError = null, Action onCompleted = null)
        {
            Task = UpdateCatalogAsync(onError, onCompleted);
        }
        
        private static async UniTask UpdateCatalogAsync(Action<Exception> onError, Action onCompleted)
        {
            var catalogsToUpdate = new List<string>();
            var checkCatalogAsync = AddressableAssets.Addressables.CheckForCatalogUpdates();

            try
            {
                if (!checkCatalogAsync.IsDone)
                    await checkCatalogAsync.ToUniTask();
            }
            catch (OperationCanceledException ex)
            {
                PrintLog.Info(LogTag.DownloadAssets, ex.Message);
                onError.Invoke(ex);
                return;
            }

            if (checkCatalogAsync.IsValid() && checkCatalogAsync.Status == AsyncOperationStatus.Succeeded)
                catalogsToUpdate = checkCatalogAsync.Result;

            if (catalogsToUpdate.Count > 0)
            {
                PrintLog.Info(LogTag.DownloadAssets, $"need to update catalogs: ‎" + $"{catalogsToUpdate.Log()}");

                try
                {
                    var updateCatalogOperation = AddressableAssets.Addressables.UpdateCatalogs();
                    await updateCatalogOperation.ToUniTask();
                }
                catch (OperationCanceledException ex)
                {
                    PrintLog.Info(LogTag.DownloadAssets, ex.Message);
                    onError.Invoke(ex);
                    return;
                }
                onCompleted?.Invoke();
            }
        }
    }
}