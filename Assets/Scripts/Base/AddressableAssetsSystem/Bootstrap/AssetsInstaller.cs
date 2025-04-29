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

using AddressableAssetsSystem.Data;
using AddressableAssetsSystem.Registry;
using AddressableAssetsSystem.Rule;
using AddressableAssetsSystem.Services;
using AddressableAssetsSystem.Utils;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject;

namespace AddressableAssetsSystem.Bootstrap
{
    public class AssetsInstaller : MonoInstaller
    {
        [SerializeField] private AssetPacksCatalogRegistry _assetPackSettingsRegistry;
        [SerializeField] private AssetPacksCatalogRegistryReference _assetPackSettingsRegistryReference;
        public override void InstallBindings()
        {
            Container.BindInstance(_assetPackSettingsRegistry);
            Container.BindInstance(_assetPackSettingsRegistryReference);
            Container.InstallAsSingle<AddressableAssetsService>();
            Container.InstallAsSingle<AssetsService>();
            Container.InstallAsSingle<DownloadAssetsService>();
            Container.InstallAsSingle<AssetsModel>();
            
            Container.InstallAsSingle<DownloadActualAssetsRule>();
        }
    }
}

