using System;
using AddressableAssetsSystem.Data;
using UnityEngine.AddressableAssets;

namespace AddressableAssetsSystem.Registry
{
    [Serializable]
    internal class AssetPacksCatalogRegistryReference : AssetReferenceT<AssetPacksCatalogRegistry>
    {
        public AssetPacksCatalogRegistryReference(string guid) : base(guid)
        { }
    }
}