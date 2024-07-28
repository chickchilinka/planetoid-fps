using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CsvImport;
using Registry;
using UniRx;
using UnityEditor;
using UnityEngine;
using Utils;
using Utils.Extensions;

namespace CSVImport.Editor
{
    public static class ImportUtil
    {
        public static void Import(GoogleSheetsImporter importer)
        {
            ImportSelected(importer);
        }
        
        private static async void ImportSelected(GoogleSheetsImporter importer)
        {
            var importedSettings = new ReactiveProperty<int>(0);
            var disposables = new CompositeDisposable();

            var successCount = 0;
            var totalCount = importer.Settings.Length;
            var parserSettings = BuildParserSettings(importer.Parsers);
            var parser = new CsvImportParser(parserSettings);

            importedSettings
                .Where(x => x == totalCount)
                .Subscribe(_ =>
                {
                    EditorUtility.ClearProgressBar();
                    EditorUtility.DisplayDialog("Google Sheets Importer",
                        $"Imported {successCount} of {totalCount} files\nErrors: {parser.ErrorCount}, Warnings: {parser.WarningCount}", "ok");
                    disposables.Clear();
                })
                .AddTo(disposables);

            for (int i = 0; i < totalCount; i++)
            {
                var settings = importer.Settings[i];

                if (!settings.IncludeInImport || settings.Registry == null || string.IsNullOrEmpty(settings.Url))
                {
                    importedSettings.Value++;
                    continue;
                }

                var urlString = settings.Url;
                var num = urlString.IndexOf("#gid=") - urlString.IndexOf("/edit") - "/edit".Length;
                var urlReq = urlString.Remove(urlString.IndexOf("/edit") + "/edit".Length, num).Replace("/edit#", "/export?format=csv&");

#pragma warning disable 618
                ObservableWWW.GetWWW(urlReq)
#pragma warning restore 618
                    .ThrottleFrame(2)
                    .Subscribe(x =>
                    {
                        EditorUtility.DisplayProgressBar("CsvImport", "Importing " + settings.Registry, importedSettings.Value / (float)totalCount);
                        if (ParseRegistry(parser, settings.Registry, x.text)) 
                            successCount++;
                        
                        importedSettings.Value++;
                    }, ex =>
                    {
                        parser.LogWarning($"[ImportUtils] URL: '{settings.Url}\n Exception: '{ex.ToString()}'");
                        importedSettings.Value++;
                    });

                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }
        }
        
        private static bool ParseRegistry(CsvImportParser parser, ScriptableObject registryObject, string csvObject)
        {
            var isRegistryList = registryObject is IRegistryList;
            var isRegistry = registryObject is IRegistryClass;
            var parseResult = false;

            if (!isRegistryList && !isRegistry)
            {
                Debug.Log("Registry: " + registryObject + " is not a valid registry");
                return false;
            }

            var serializedRegistry = new SerializedObject(registryObject);

            if (isRegistryList)
            {
                var settings = serializedRegistry.FindProperty("_registryItems");
                settings.ClearArray();

                Debug.Log("Importing <color=white>" + registryObject + "</color> as array registry");
                parseResult = parser.ImportArrayFromCsvFile(csvObject, serializedRegistry);
            }
            else
            {
                Debug.Log("Importing <color=white>" + registryObject + "</color> as single registry");
                parseResult = parser.ImportFromCsvFile(csvObject, serializedRegistry);
            }

            if (parseResult)
            {
                serializedRegistry.ApplyModifiedProperties();
                EditorUtility.SetDirty(registryObject);
            }

            return parseResult;
        }
        
        private static IEnumerable<CsvImportParserSettings> BuildParserSettings(GoogleSheetsParserSettings[] parserSettings)
        {
            if (parserSettings == null) 
                return null;
            
            var parsers = new List<CsvImportParserSettings>();
            for (int i = 0; i < parserSettings.Length; i++)
            {
                Debug.Log($"Adding parser {parserSettings[i].ParserTypeName} with fields {parserSettings[i].ParserFields.Log()}");
                parsers.Add(new CsvImportParserSettings { ParserTypeName = parserSettings[i].ParserTypeName, ParserFields = parserSettings[i].ParserFields});
            }
            return parsers;
        }
    }
}