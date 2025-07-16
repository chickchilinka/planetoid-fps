using System.Collections.Generic;
using System.IO;
using System.Linq;
using Registry;
using UnityEditor;
using UnityEngine;

namespace CsvImport
{
    [CustomEditor(typeof(CsvImportSetup))]
    public class CsvImportSetupEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            if (GUILayout.Button("Import Selected"))
            {
                var settings = serializedObject.FindProperty("Settings");
                var successCount = 0;
                var totalCount = settings.arraySize;

                var parserSettings = BuildParserSettings(serializedObject.FindProperty("Parsers"));
                
                var parser = new CsvImportParser(parserSettings);

                for (int i = 0; i < totalCount; i++)
                {
                    var setup = settings.GetArrayElementAtIndex(i);
                    var include = setup.FindPropertyRelative("IncludeInImport");
                    var registry = setup.FindPropertyRelative("Registry");
                    var csvFile = setup.FindPropertyRelative("CsvFile");

                    var registryObject = registry.objectReferenceValue as ScriptableObject;
                    var csvFileObject = csvFile.objectReferenceValue as TextAsset;

                    if (!include.boolValue || registryObject == null || csvFileObject == null) continue;
                    
                    EditorUtility.DisplayProgressBar("CsvImport", "Importing " + registryObject, i / (float) totalCount);

                    if (ParseRegistry(parser, registryObject, csvFileObject))
                    {
                        successCount++;
                    }
                }
                
                EditorUtility.ClearProgressBar();

                EditorUtility.DisplayDialog("CsvImport",
                    string.Format("Imported {0} of {1} files\nErrors: {3}, Warnings: {2}", successCount, totalCount,
                        parser.WarningCount, parser.ErrorCount), "ok");
            }
        }

        private IEnumerable<CsvImportParserSettings> BuildParserSettings(SerializedProperty parserProperty)
        {
            if (parserProperty == null) return null;
            var parsers = new List<CsvImportParserSettings>();
            for (int i = 0; i < parserProperty.arraySize; i++)
            {
                var settings = parserProperty.GetArrayElementAtIndex(i);
                var parserNameProperty = settings.FindPropertyRelative("ParserTypeName").stringValue;
                var parserFieldsProperty = settings.FindPropertyRelative("ParserFields");
                
                var parserFields = GetStringArray(parserFieldsProperty);
                
                if (string.IsNullOrEmpty(parserNameProperty) || parserFields == null || parserFields.Length == 0)
                {
                    Debug.LogWarning(string.Format("Invalid parser at index {0}, skipping", i));
                    continue;
                }
                Debug.Log(string.Format("Adding parser {0} with fields {1}", parserNameProperty, string.Join(",", parserFields)));
                parsers.Add(new CsvImportParserSettings {ParserTypeName = parserNameProperty, ParserFields = parserFields});
            }
            return parsers;
        }

        private string[] GetStringArray(SerializedProperty property)
        {
            var fields = new List<string>();
            for (int i = 0; i < property.arraySize; i++)
            {
                fields.Add(property.GetArrayElementAtIndex(i).stringValue);
            }
            return fields.ToArray();
        }

        private static bool ParseRegistry(CsvImportParser parser, ScriptableObject registryObject, TextAsset csvFileObject)
        {
            var isRegistryList = registryObject is IRegistryList;
            var isRegistry = registryObject is IRegistry;

            var parseResult = false;
            
            if (!isRegistryList && !isRegistry)
            {
                Debug.Log("<color=red>Registry:</color> " + registryObject + " is not a valid registry");
                return false;
            }
            
            var serializedRegistry = new SerializedObject(registryObject, null);

            if (isRegistryList)
            {
                Debug.Log("Importing " + registryObject + " as array registry");
                parseResult = parser.ImportArrayFromCsvFile(csvFileObject, serializedRegistry);
            }
            else
            {
                Debug.Log("Importing " + registryObject + " as single registry");
                parseResult = parser.ImportFromCsvFile(csvFileObject, serializedRegistry);
            }

            if (parseResult)
            {
                serializedRegistry.ApplyModifiedProperties();
                EditorUtility.SetDirty(registryObject);
            }

            return parseResult;
        }
    }
}