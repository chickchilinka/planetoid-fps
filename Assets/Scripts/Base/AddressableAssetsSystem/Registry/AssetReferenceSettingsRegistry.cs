using System;
using AddressableAssetsSystem.Attributes;
using Base.Common.Registry;
using UnityEngine;

namespace AddressableAssetsSystem.Data
{
    [Serializable]
    [CreateAssetMenu(fileName = "AssetsReferenceSettingsRegistry", menuName = "Registry/Settings/Local/Assets/AssetReferences")]
    public class AssetReferenceSettingsRegistry : RegistryListBase<AssetReferenceSettings>
    {
#if UNITY_EDITOR
        [AttributeAssetLabel]
#endif
        public string[] Labels;
    }
}