using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Base.Common.Utils.Extensions
{
    public static class LogExtensions
    {
        public static string LogDictionary<TKey, TValue>(this Dictionary<TKey, TValue> dict, 
            Func<TKey, string> keyToString = null,
            Func<TValue, string> valueToString = null)
        {
            string msg = string.Empty;

            foreach (var p in dict)
            {
                var key = keyToString?.Invoke(p.Key) ?? p.Key.ToString();
                var value = valueToString?.Invoke(p.Value) ?? p.Value.ToString();
                
                msg += $"[{key}]: {value}, ";
            }

            return msg;
        }
        
        public static string Log<T>(this IEnumerable<T> enumerable, Func<T, string> getLog = null, string separator = ", ")
        {
            var result = string.Empty;

            if (enumerable.IsNullOrEmpty())
                return result;

            foreach (var t in enumerable)
            {
                if (t == null)
                    result += "null" + separator;
                else
                    result += (getLog == null ? t.ToString() : getLog.Invoke(t)) + separator;
            }

            result = result.Remove(result.Length - separator.Length, separator.Length);

            return result;
        }

        public static string GetPath(this GameObject gameObject)
        {
            if (gameObject == null)
                return String.Empty;

            var parent = gameObject.transform.parent;
            var path = new List<string>();
            
            while (parent != null)
            {
                path.Add(parent.name);

                parent = parent.transform.parent;
            }
            
            var result = string.Empty;
            
            for (int i = path.Count - 1; i >= 0; i--)
                result += path[i] + "/";
            
            result += gameObject.name;

            return result;
        }
        
        public static bool AreArraysEqual<T>(this T[] array1, T[] array2, Func<T, string> getLog = null) where T : class 
        {
            if (array1 == null && array2 == null)
                return true;
            
            if (array1 == null || array2 == null || 
                array1.Count() != array2.Count())
                return false;

            for (int i = 0; i < array1.Length; i++)
            {
                var item1 = array1[i];
                var item2 = array2[i];

                if (!item1.Equals(item2))
                    return false;
            }

            return true;
        }
    }
}
