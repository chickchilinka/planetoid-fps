using System;
using AddressableAssetsSystem.Data;
using Base.Common.Registry;

namespace AddressableAssetsSystem.Registry
{
    [Serializable]
    public class AssetPackSettings : IRegistryData
    {
        public string Id => AssetReferenceSettingsRegistry.GetInstanceID().ToString();
        public AssetReferenceSettingsRegistry AssetReferenceSettingsRegistry;
    }
}