using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CsvImport;
using Registry;
using UniRx;
using UnityEditor;
using UnityEngine;
using Utils.Debugger;

namespace Utils
{
    [CustomEditor(typeof(GoogleSheetsImporter))]
    public class GoogleSheetsImporterEditor : Editor
    {
        private readonly ReactiveProperty<int> _count = new ReactiveProperty<int>();
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        private const float Heigth = 30f;
        private const int ButtonsInRow = 4;

        private readonly Dictionary<string, int[]> _presets = new Dictionary<string, int[]>
        {
            {"Instruments", new[] {0, 1, 2, 5}},
            {"TourBus", new[] {2, 3, 4}},
        };
        
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();

            if (GUILayout.Button("Import Selected", GUILayout.Height(40), GUILayout.ExpandWidth(true)))
                ImportSelected();

            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Delete Selected", GUILayout.Height(Heigth), GUILayout.ExpandWidth(true)))
            {
                var settings = serializedObject.FindProperty("Settings");
                var totalCount = settings.arraySize;
                var indexesToDelete = new List<int>();

                for (int i = 0; i < totalCount; i++)
                {
                    var setup = settings.GetArrayElementAtIndex(i);
                    var include = setup.FindPropertyRelative("IncludeInImport");

                    if (include.boolValue)
                        indexesToDelete.Add(i);
                }
                
                indexesToDelete.ForEach(i => settings.DeleteArrayElementAtIndex(i));
                
                serializedObject.ApplyModifiedProperties();
            }
            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("Select All", GUILayout.Height(Heigth), GUILayout.ExpandWidth(true)))
            {
                SelectAll(true);
            }

            if (GUILayout.Button("Deselect All", GUILayout.Height(Heigth), GUILayout.ExpandWidth(true)))
            {
                SelectAll(false);
            }
            EditorGUILayout.EndHorizontal();

#if DEF_LEPPARD
            InitializePresets();
#endif

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Update registries view", GUILayout.Height(Heigth), GUILayout.ExpandWidth(true)))
            {
                var settings = serializedObject.FindProperty("Settings");

                UpdateNames(settings);
                serializedObject.ApplyModifiedProperties();

                SortByNames(settings);
                serializedObject.ApplyModifiedProperties();

            }

            EditorGUILayout.EndHorizontal();
        }

        private void UpdateNames(SerializedProperty serializedProperty)
        {
            var totalCount = serializedProperty.arraySize;

            for (int i = 0; i < totalCount; i++)
            {
                var setup = serializedProperty.GetArrayElementAtIndex(i);
                var registry = setup.FindPropertyRelative("Registry");
                var name = setup.FindPropertyRelative("Name");

                if (registry != null && name != null)
                {
                    var value = registry.objectReferenceValue.ToString();
                    if (value.Contains(" "))
                        name.stringValue = value.Substring(0, value.IndexOf(" "));
                }
            }
        }
        
        private void SortByNames(SerializedProperty serializedProperty)
        {
        }

        private void InitializePresets()
        {
            EditorGUILayout.Space();

            var i = 1;
            
            EditorGUILayout.BeginHorizontal();

            foreach (var preset in _presets)
            {
                if (GUILayout.Button(preset.Key, GUILayout.Height(Heigth), GUILayout.ExpandWidth(true)))
                    Select(preset.Value, true);

                if (i % ButtonsInRow == 0)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }

                i++;
            }

            EditorGUILayout.EndHorizontal();
        }

        private async void ImportSelected()
        {
            _count.Value = 0;

            var settings = serializedObject.FindProperty("Settings");
            var successCount = 0;
            var totalCount = settings.arraySize;
            var parserSettings = BuildParserSettings(serializedObject.FindProperty("Parsers"));
            var parser = new CsvImportParser(parserSettings);

            _count
                .Where(x => x == totalCount)
                .Subscribe(_ =>
                {
                    EditorUtility.ClearProgressBar();
                    EditorUtility.DisplayDialog("GoogleSheetsImporter",
                        string.Format("Imported {0} of {1} files\nErrors: {3}, Warnings: {2}", successCount, totalCount,
                            parser.WarningCount, parser.ErrorCount), "ok");
                    _disposables.Clear();
                })
                .AddTo(_disposables);

            for (int i = 0; i < totalCount; i++)
            {
                var setup = settings.GetArrayElementAtIndex(i);
                var include = setup.FindPropertyRelative("IncludeInImport");
                var registry = setup.FindPropertyRelative("Registry");
                var url = setup.FindPropertyRelative("Url");

                var registryObject = registry.objectReferenceValue as ScriptableObject;

                if (!include.boolValue || registryObject == null || string.IsNullOrEmpty(url.stringValue))
                {
                    _count.Value++;
                    continue;
                }

                var urlString = url.stringValue;
                var num = urlString.IndexOf("#gid=") - urlString.IndexOf("/edit") - "/edit".Length;
                var urlReq = urlString.Remove(urlString.IndexOf("/edit") + "/edit".Length, num).Replace("/edit#", "/export?format=csv&");

#pragma warning disable 618
                ObservableWWW.GetWWW(urlReq)
#pragma warning restore 618
                    .ThrottleFrame(2)
                    .Subscribe(x =>
                    {
                        EditorUtility.DisplayProgressBar("CsvImport", "Importing " + registryObject, _count.Value / (float)totalCount);
                        if (ParseRegistry(parser, registryObject, x.text)) 
                            successCount++;
                        
                        _count.Value++;
                    }, ex =>
                    {
                        PrintLog.Warn($"[CsvImportUtils] URL: '{url.stringValue}\n Exception: '{ex.ToString()}'");
                        _count.Value++;
                    });

                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }
        }
        
        private void SelectAll(bool arg)
        {
            _count.Value = 0;

            var settings = serializedObject.FindProperty("Settings");
            var totalCount = settings.arraySize;

            for (int i = 0; i < totalCount; i++)
            {
                var setup = settings.GetArrayElementAtIndex(i);
                var include = setup.FindPropertyRelative("IncludeInImport");

                include.boolValue = arg;
            }

            serializedObject.ApplyModifiedProperties();
        }
        
        private void Select(int[] indexes, bool activity)
        {
            _count.Value = 0;

            var settings = serializedObject.FindProperty("Settings");
            var totalCount = settings.arraySize;

            for (int i = 0; i < totalCount; i++)
            {
                var setup = settings.GetArrayElementAtIndex(i);
                var include = setup.FindPropertyRelative("IncludeInImport");

                include.boolValue = indexes.Contains(i) ? activity : !activity;
            }

            serializedObject.ApplyModifiedProperties();
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
                Debug.Log(string.Format("<i>Adding parser {0} with fields {1}</i>", parserNameProperty, string.Join(",", parserFields)));
                parsers.Add(new CsvImportParserSettings { ParserTypeName = parserNameProperty, ParserFields = parserFields });
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

        private static bool ParseRegistry(CsvImportParser parser, ScriptableObject registryObject, string csvObject)
        {
            var isRegistryList = registryObject is IRegistryList;
            var isRegistry = registryObject is IRegistry;
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
    }
}