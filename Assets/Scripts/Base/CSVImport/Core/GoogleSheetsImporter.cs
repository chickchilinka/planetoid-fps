using System;
using System.Linq;
using UnityEngine;

namespace Utils
{
    [CreateAssetMenu(fileName = "GoogleSheetsImporter", menuName = "Registry/Google Sheets Importer")]
    [Serializable]
    public class GoogleSheetsImporter : ScriptableObject
    {
        [SerializeField] public GoogleSheetsParserSettings[] Parsers;
        [SerializeField] public GoogleSheetsImporterSettings[] Settings;

        public void SelectAll()
        {
            foreach (var settings in Settings)
                settings.IncludeInImport = true;
        }
        
        public void Select(params string[] registryNamesToImport)
        {
            foreach (var settings in Settings)
                settings.IncludeInImport = registryNamesToImport.Contains(settings.Registry.name);
        }
    }

    [Serializable]
    public class GoogleSheetsImporterSettings
    {
        public string Name;
        public bool IncludeInImport;
        public ScriptableObject Registry;
        public string Url;
    }

    [Serializable]
    public class GoogleSheetsParserSettings
    {
        public string ParserTypeName;
        public string[] ParserFields;
    }
}