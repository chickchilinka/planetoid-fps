using System;
using UnityEngine;

namespace CsvImport
{
    [Serializable]
    public class CsvImportSettings
    {
        public bool IncludeInImport;
        public ScriptableObject Registry;
        public TextAsset CsvFile;
    }
    
    [Serializable]
    public class CsvImportParserSettings
    {
        public string ParserTypeName;
        public string [] ParserFields;
    }
    
    [CreateAssetMenu(fileName = "CSVImportSetup", menuName = "Registry/CSV Import Setup")]
    [Serializable]
    public class CsvImportSetup : ScriptableObject
    {
        public CsvImportParserSettings[] Parsers;
        public CsvImportSettings[] Settings;
    }
}