using System;
using AddressableAssetsSystem.Attributes;
using General.Data;
using UnityEngine;

namespace AddressableAssetsSystem.Data
{
    [Serializable]
    [CreateAssetMenu(fileName = "AssetsReferenceSettingsRegistry", menuName = "Registry/Settings/Local/Assets/AssetReferences")]
    public class AssetReferenceSettingsRegistry : BaseGameSettingsList<AssetReferenceSettings>
    {
#if UNITY_EDITOR
        [AttributeAssetLabel]
#endif
        public string[] Labels;
    }
}