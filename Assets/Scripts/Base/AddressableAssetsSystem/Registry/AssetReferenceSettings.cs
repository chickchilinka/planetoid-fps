using System;
using Base.Common.Registry;

namespace AddressableAssetsSystem.Data
{
    [Serializable]
    public class AssetReferenceSettings : IRegistryData
    {
        public string Id => Name;
        
        public string Name;
        public UnityEngine.AddressableAssets.AssetReference AssetReference;
    }
}