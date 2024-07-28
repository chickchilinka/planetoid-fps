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