using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using ModestTree;
using UnityEditor;
using UnityEngine;

namespace CsvImport
{
    #if UNITY_EDITOR
    
    public class CsvImportParser
    {
        private static readonly char CsvSeparator = ',';
        private static readonly char FieldSeparator = ';';
        private static readonly string BaseDataPropertyName = "RegistryData";
        private static readonly string BaseArrayPropertyName = "_registryItems";

        private readonly Dictionary<string, Action<SerializedProperty, string>> _parsers;

        public int WarningCount { get; private set; }
        public int ErrorCount { get; private set; }

        public CsvImportParser(IEnumerable<CsvImportParserSettings> customParsers = null)
        {
            _parsers = BaseParsers.ToDictionary(data => data.Key, data => data.Value);
            if (customParsers != null) 
                AddCustomParsers(_parsers, customParsers);
        }

        private void AddCustomParsers(Dictionary<string, Action<SerializedProperty, string>> parsers, IEnumerable<CsvImportParserSettings> customParsers)
        {
            foreach (var parser in customParsers)
            {
                parsers[parser.ParserTypeName] = (property, value) => ParseProperty(property,
                    BuildPropertyData(parser.ParserFields, value.Split(FieldSeparator)));
            }
        }

        private static readonly Dictionary<string, Action<SerializedProperty, string>> BaseParsers =
            new Dictionary<string, Action<SerializedProperty, string>>
            {
                {
                    "float",
                    (property, value) => property.floatValue =
                        float.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture)
                },
                {
                    "int",
                    (property, value) => property.intValue = int.Parse(value)
                },
                {
                    "long",
                    (property, value) => property.longValue = long.Parse(value)
                },
                {
                    "double",
                    (property, value) => property.doubleValue =
                        double.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture)
                },
                {
                    "string",
                    (property, value) => property.stringValue = value
                },
                {
                    "bool",
                    (property, value) =>
                        property.boolValue = string.CompareOrdinal(value.ToLowerInvariant(), "true") == 0
                },
                {
                    "Enum",
                    (property, value) =>
                    {
                        var enumIndex = Array.IndexOf(property.enumNames, value);
                        if (enumIndex < 0) throw new InvalidOperationException(string.Format("Enum value '{0}' for type {1} not found", value, property.propertyType));
                        property.enumValueIndex = enumIndex;
                    }
                }
            };

        private static SerializedProperty GetBaseArrayProperty(SerializedObject serializedObject)
        {
            var property = serializedObject.FindProperty(BaseArrayPropertyName);
            Assert.That(property != null && property.isArray, BaseArrayPropertyName + " property exists and is array");
            return property;
        }

        private static SerializedProperty GetBaseProperty(SerializedObject serializedObject)
        {
            var property = serializedObject.FindProperty(BaseDataPropertyName);
            Assert.That(property != null, BaseDataPropertyName + " property exists");
            return property;
        }

        private bool ParseArrayProperty(SerializedProperty arrayProperty, int arrayIndex,
            Dictionary<string, string> data)
        {
            if (arrayProperty.arraySize <= arrayIndex) arrayProperty.InsertArrayElementAtIndex(arrayIndex);

            var property = arrayProperty.GetArrayElementAtIndex(arrayIndex);
            return ParseProperty(property, data);
        }

        private bool ParseProperty(SerializedProperty property, Dictionary<string, string> data)
        {
            foreach (var entry in data)
            {
                var propertyName = entry.Key;
                var propertyValue = entry.Value;
                var isArray = propertyName.EndsWith("[]");
                var result = true;

                //                if (string.IsNullOrEmpty(propertyValue))
                //                    Debug.LogWarning($"The value of {propertyName} property is <b>empty</b>!");

                if (!isArray)
                    result = ParseChildSimpleProperty(property, propertyName, propertyValue);
                else
                    result = ParseChildArrayProperty(property, propertyName, propertyValue);

                //if (!result) return false;
            }

            return true;
        }

        private bool ParseChildSimpleProperty(SerializedProperty property, string propertyName,
            string propertyValue)
        {
            var innerProperty = property.FindPropertyRelative(propertyName);
            if (innerProperty == null)
            {
                LogWarning(string.Format("[CsvImportUtils] property with name '<color=orange>{0}</color>' not found in '<color=orange>{1}</color>'",
                    propertyName, property.type));
                return false;
            }

            if (!SetPropertyValue(innerProperty, propertyValue)) return false;
            return true;
        }

        public bool ParseChildArrayProperty(SerializedProperty property, string propertyName, string propertyValue)
        {
            propertyName = propertyName.Substring(0, propertyName.Length - 2);

            var childProperty = property.FindPropertyRelative(propertyName);
            var values = Split(propertyValue);

            if (childProperty == null)
            {
                LogWarning($"Child property is null for {propertyName}");
                return false;
            }
            
            if (childProperty.isArray)
                childProperty.ClearArray();

            values = values.Where(n => !string.IsNullOrEmpty(n)).ToArray();

            if (values.Length == 0) return true;

            for (var arrayIndex = 0; arrayIndex < values.Length; arrayIndex++)
            {
                var value = values[arrayIndex];
                if (childProperty.arraySize <= arrayIndex) childProperty.InsertArrayElementAtIndex(arrayIndex);
                var innerProperty = childProperty.GetArrayElementAtIndex(arrayIndex);

                if (innerProperty == null)
                {
                    LogWarning(string.Format("[CsvImportUtils] property at index {0} for array {1} is undefined",
                        arrayIndex, childProperty));
                    continue;
                }

                if (!SetPropertyValue(innerProperty, value)) return false;
            }

            return true;
        }

        public void ResetCounters()
        {
            WarningCount = 0;
            ErrorCount = 0;
        }

        public void LogWarning(string text)
        {
            WarningCount++;
            Debug.LogWarning(text);
        }

        public void LogError(string text)
        {
            ErrorCount++;
            Debug.LogError(text);
        }

        public bool SetPropertyValue(SerializedProperty property, string propertyValue)
        {
            //Debug.Log(string.Format("Set property {0} of type {2} value = {1}", property, propertyValue, property.type));

            //if (!string.IsNullOrEmpty(propertyValue)) return false; // !!! could be an empty field

            if (_parsers.ContainsKey(property.type))
            {
                try
                {
                    _parsers[property.type].Invoke(property, propertyValue);
                }
                catch (Exception e)
                {
                    var value = string.IsNullOrEmpty(propertyValue) ? "EMPTY" : propertyValue;
                    LogWarning($"[CsvImportUtils] can't parse <color=white><b>{ value }</b></color> property for {property.name} (<i>{ e.Message } </i>)");
                    return false;
                }

                return true;
            }

            LogWarning(string.Format(
                "[CsvImportUtils] can't parse property '{0}', unknown parser for type '<color=orange>{1}</color>' and value '{2}'",
                property.propertyPath, property.type, propertyValue));

            return false;
        }

        private static string[] ParseHeaders(string line)
        {
            //Debug.Log("[CsvImportUtils] parsing header: " + line);
            var headers = Split(line);
            if (headers.Length == 0) return null;
            return headers;
        }

        public bool ImportArrayFromCsvFile(TextAsset csvFile, SerializedObject serializedObject)
        {
            var csvContent = csvFile.text.Split('\n');
            return ImportArrayFromData(serializedObject, csvContent);
        }

        public bool ImportArrayFromCsvFile(string csvData, SerializedObject serializedObject)
        {
            var csvContent = csvData.Split('\n');
            return ImportArrayFromData(serializedObject, csvContent);
        }

        public bool ImportFromCsvFile(TextAsset csvFile, SerializedObject serializedObject)
        {
            var csvContent = csvFile.text.Split('\n');
            return ImportFromData(serializedObject, csvContent);
        }

        public bool ImportFromCsvFile(string csvData, SerializedObject serializedObject)
        {
            var csvContent = csvData.Split('\n');
            return ImportFromData(serializedObject, csvContent);
        }

        public bool ImportArrayFromData(SerializedObject serializedObject,
            IEnumerable<string> csvData)
        {
            if (csvData == null) return false;
            var serializedArray = GetBaseArrayProperty(serializedObject);

            var lines = csvData.GetEnumerator();
            if (!lines.MoveNext()) return false;

            var headerNames = ParseHeaders(lines.Current);
            if (headerNames == null || headerNames.Length == 0)
            {
                LogError("[CsvImportUtil] Invalid header");
                lines.Dispose();
                return false;
            }

            var lineCounter = 0;
            var arrayIndex = 0;

            while (lines.MoveNext())
            {
                var line = lines.Current;
                //Debug.Log("[CsvImportUtils] parsing line: " + line);
                lineCounter++;
                if (string.IsNullOrEmpty(line)) continue;

                var values = Split(line);

                var propertyData = BuildPropertyData(headerNames, values);
                if (!ParseArrayProperty(serializedArray, arrayIndex, propertyData)) continue;

                arrayIndex++;
            }

            //Debug.Log(string.Format("[CsvImportUtil] Imported {1} of {0} lines", lineCounter, arrayIndex));

            lines.Dispose();
            return true;
        }

        private bool ImportFromData(SerializedObject serializedObject, IEnumerable<string> csvData)
        {
            if (csvData == null) return false;
            var serializedProperty = GetBaseProperty(serializedObject);

            var lines = csvData.GetEnumerator();
            if (!lines.MoveNext()) return false;

            if (lines.Current == null)
            {
                LogError("[CsvImportUtil] Invalid header");
                lines.Dispose();
                return false;
            }

            var lineCounter = 0;
            var arrayIndex = 0;

            var headerNames = new List<string>();
            var headerValues = new List<string>();

            while (lines.MoveNext())
            {
                var line = lines.Current;
                //Debug.Log("[CsvImportUtils] parsing line: " + line);
                lineCounter++;
                if (string.IsNullOrEmpty(line)) continue;

                var parts = Split(line);
                if (parts.Length < 2) continue;

                var propertyName = parts[0].Trim();
                var propertyValue = parts[1].Trim();

                headerNames.Add(propertyName);
                headerValues.Add(propertyValue);

                arrayIndex++;
            }

            var propertyData = BuildPropertyData(headerNames.ToArray(), headerValues.ToArray());

            ParseProperty(serializedProperty, propertyData);
            //Debug.Log(string.Format("[CsvImportUtil] Imported {1} of {0} lines", lineCounter, arrayIndex));

            lines.Dispose();
            return true;
        }

        private Dictionary<string, string> BuildPropertyData(string[] headerNames, string[] values)
        {
            var data = new Dictionary<string, string>();
            
            for (var i = 0; i < values.Length; i++)
            {
                if (i >= headerNames.Length) 
                    break;

                var name = headerNames[i].Trim();
                var value = values[i].Trim();

                if (string.IsNullOrEmpty(name))
                {
                    LogError($"[CsvImportUtil] invalid header name for value {value}, empty not allowed");
                    continue;
                }

                //if (string.IsNullOrEmpty(value)) continue; // !!! could be an empty field

                // array value
                if (name.EndsWith("[]"))
                    if (data.ContainsKey(name))
                        value = data[name] + CsvSeparator + value;

                data[name] = value;
            }

            return data;
        }

        private static readonly Regex RexCsvSplitter = new Regex(@",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))");

        private static string[] Split(string value)
        {
#if UNITY_EDITOR_OSX
            var envDelimiter = "\r".ToCharArray();
#else
                var envDelimiter = Environment.NewLine.ToCharArray();
#endif
            
            var valu = value.TrimEnd(envDelimiter);
            var result = RexCsvSplitter.Split(valu);

            for (int i = 0; i < result.Length; i++)
                result[i] = Unescape(result[i]);

            //Debug.Log("Result : " + result.Aggregate((a, b) => a + " | " + b));

            return result;
        }

        private const string QUOTE = "\"";
        private const string ESCAPED_QUOTE = "\"\"";
        private static char[] CHARACTERS_THAT_MUST_BE_QUOTED = { ',', '"', '\n' };

        public static string Escape(string s)
        {
            if (s.Contains(QUOTE))
                s = s.Replace(QUOTE, ESCAPED_QUOTE);

            if (s.IndexOfAny(CHARACTERS_THAT_MUST_BE_QUOTED) > -1)
                s = QUOTE + s + QUOTE;

            return s;
        }

        public static string Unescape(string s)
        {
            try
            {
                if (s.StartsWith(QUOTE) && s.EndsWith(QUOTE))
                {
                    s = s.Substring(1, s.Length - 2);

                    if (s.Contains(ESCAPED_QUOTE))
                        s = s.Replace(ESCAPED_QUOTE, QUOTE);
                }
            }
            catch (Exception)
            {
                Debug.LogError($"There are some enter in the sheet row");
            }

            return s;
        }

    }
    
    #endif
}