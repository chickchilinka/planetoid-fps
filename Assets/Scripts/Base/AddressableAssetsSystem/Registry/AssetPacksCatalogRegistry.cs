using System;
using System.Collections.Generic;
using System.Linq;
using AddressableAssetsSystem.Registry;
using General.Data;
using UnityEngine;

namespace AddressableAssetsSystem.Data
{
    [Serializable]
    [CreateAssetMenu(fileName = "AssetPackSettingsRegistry", menuName = "Registry/Settings/Local/Assets/AssetPacks")]
    internal class AssetPacksCatalogRegistry : BaseGameSettingsList<AssetPackSettings>
    {
        public AssetPackSettings GetAssetPackSettingsById(string id)
        {
            return GetById(id);
        }
        
        public AssetReferenceSettings GetAssetReferenceSettingsByName(string id)
        {
            var items = GetItems();

            foreach (var assetPackSettings in items)
            {
                var assetsReferenceSettings = assetPackSettings.AssetReferenceSettingsRegistry.GetById(id);

                if (assetsReferenceSettings != null)
                    return assetsReferenceSettings;
            }

            Debug.LogError($"Asset with Id: {id} not found");
            return null;
        }
        
        public bool HasAssetReferenceSettingsWithName(string id)
        {
            var settings =Array.Find(GetItems(),
                setting => setting.AssetReferenceSettingsRegistry.GetById(id) != null);
            return settings != null;
        }


        public List<AssetReferenceSettings> GetSettingsesByLabel(string assetLabel)
        {
            var items = GetItems();
            var list = new List<AssetReferenceSettings>();
            
            foreach (var item in items)
            {
                if (item.AssetReferenceSettingsRegistry.Labels.Contains(assetLabel))
                {
                    list.AddRange(item.AssetReferenceSettingsRegistry.GetItems());
                }
            }
            if (list.Count == 0)
                Debug.LogError($"Asset with Type: {assetLabel} not found");
            
            return list;
        }
        
        public List<AssetReferenceSettings> GetAllAssets()
        {
            var items = GetItems();
            var list = new List<AssetReferenceSettings>();
            
            foreach (var item in items)
            {
                list.AddRange(item.AssetReferenceSettingsRegistry.GetItems());
            }

            return list;
        }
    }
}