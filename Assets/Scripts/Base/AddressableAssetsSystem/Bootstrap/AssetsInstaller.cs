using AddressableAssetsSystem.Data;
using AddressableAssetsSystem.Registry;
using AddressableAssetsSystem.Services;
using AddressableAssetsSystem.Utils;
using UnityEngine;
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
        }
    }
}

