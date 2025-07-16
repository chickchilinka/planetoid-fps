using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AddressableAssetsSystem.Data;
using UnityEditor;
using UnityEngine;

namespace AddressableAssetsSystem.Editor
{
    [CustomEditor(typeof(AssetReferenceSettingsRegistry))]
    public class AssetReferenceSettingsImporterEditor : UnityEditor.Editor
    {
        private IList _dataList;
        private bool _isReadyToSave;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();

            if (GUILayout.Button("Import Assets From Folder", GUILayout.Height(40), GUILayout.ExpandWidth(true)))
                ImportAssets();
        }

        private void ImportAssets()
        {
            var importPath = EditorUtility.OpenFolderPanel("Select Assets Folder", string.Empty, string.Empty);
            var files = GetDataFromPath(importPath);

            var settingses = new List<AssetReferenceSettings>();

            foreach (var filePath in files)
            {
                var assetReference = new UnityEngine.AddressableAssets.AssetReference(AssetDatabase.AssetPathToGUID(filePath));

                var settings = new AssetReferenceSettings()
                {
                    AssetReference = assetReference,
                    Name = assetReference.editorAsset.name
                };
                
                settingses.Add(settings);
            }

            var registry = target as AssetReferenceSettingsRegistry;
            registry.SetItems(settingses.ToArray());
            
            EditorUtility.SetDirty(registry);
        }

        private List<string> GetDataFromPath(string path)
        {
            var assets = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories).Where(x => !x.Contains(".meta"))
                .Select(x => GetPath(x));

            return assets.ToList();
        }


        private string GetPath(string path)
        {
            return path.Substring(path.IndexOf("Assets/", StringComparison.Ordinal));
        }
    }
}