using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Inventory;
using UnityEngine;

namespace Utils.Extensions
{
    public static class GenericExtensions
    {
        public static Color TryParseColor(this string colorValue, Color defaultColor)
        {
            if (ColorUtility.TryParseHtmlString("#" + colorValue, out var iconColor))
                return iconColor;

            return defaultColor;
        }
    
        public static Type GetTypeAndParams<TInterface>(this string input)
            where TInterface : class
        {
            if (string.IsNullOrEmpty(input))
                return null;

            var sourceType = typeof(TInterface);
            var assembly = sourceType.Assembly;
            var typeFullName = $"{sourceType.Namespace}.{input}";
            
            return assembly.GetType(typeFullName);
        }
        
        /// <summary>
        /// Returns hue in the range 0.0 to 1.0.
        /// </summary>
        /// <param name="color"></param>
        /// <returns>Returns hue in the range 0.0 to 1.0</returns>
        public static float GetHue(this Color color)
        {
            var hue = 0f;
            var saturation = 0f;
            var value = 0f;
            
            Color.RGBToHSV(color, out hue, out saturation, out value);

            return hue;
        }

        public static string GetLastSplittedValue(this string input, char serapator)
        {
            var splitted = input.Split(serapator);
            return splitted.LastOrDefault();
        }
        
        public static T GetById<T>(this IEnumerable<T> enumerable, string id) where T : class, IIdentified
        {
            if (string.IsNullOrEmpty(id))
                return null;
            
            foreach (var t in enumerable)
                if (t.Id.Equals(id))
                    return t;

            return null;
        }
        
        public static List<string> GetIds<T>(this IEnumerable<T> enumerable) where T : class, IIdentified
        {
            var ids = new List<string>();

            if (enumerable == null)
                return ids;

            foreach (var t in enumerable)
            {
                if (t != null)
                    ids.Add(t.Id);
            }

            return ids;
        }
        
        public static bool IsIdExist<T>(this IEnumerable<T> enumerable, string id) where T : class, IIdentified
        {
            if (string.IsNullOrEmpty(id))
                return false;
            
            foreach (var t in enumerable)
                if (t.Id.Equals(id))
                    return true;

            return false;
        }
        
        public static List<T> AddElement<T>(this List<T> list, T element)
        {
            list.Add(element);
            return list;
        }
        
        public static T[] Clean<T>(this IEnumerable<T> enumerable)
        {
            return enumerable
                .Where(data => data != null)
                .ToArray();
        }

        public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable == null)
                return true;
            
            foreach (var item in enumerable)
                return false;
            
            return true;
        }

        public static HashSet<TResult> Concat<TResult, TInput>(this IEnumerable<TInput> enumerable, Func<TInput, HashSet<TResult>> getHashSet)
        {
            var result = new HashSet<TResult>();

            foreach (var data in enumerable)
                result.AddRange(getHashSet.Invoke(data));

            return result;
        }
        
        public static KeyValuePair<TKey, TValue>? GetByIndex<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, int index)
        {
            var i = 0;
            
            foreach (var data in dictionary)
            {
                if (i == index)
                    return data;

                i++;
            }

            return null;
        }

        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> enumerable)
        {
            var result = new HashSet<T>();

            foreach (var data in enumerable)
                result.Add(data);

            return result;
        }

        public static T[] RemoveAtIndex<T>(this T[] array, int index)
        {
            if (index < 0 || index >= array.Length)
            {
                Debug.LogWarning($"Invalid index {index}");
                return array;
            }

            var newArray = new T[array.Length - 1];

            var offset = 0;

            for (var i = 0; i < array.Length; i++)
            {
                if (i == index)
                {
                    offset++;
                    continue;
                }

                newArray[i - offset] = array[i];
            }

            return newArray;
        }
        
        public static int GetIndex<T>(this IEnumerable<T> enumerable, T obj)
        {
            if (enumerable == null || obj == null)
                return -1;

            var array = enumerable.ToArray();

            for (var i = 0; i < array.Length; i++)
            {
                if (array[i].Equals(obj))
                    return i;
            }

            return -1;
        }

        public static Queue<T> ToQueque<T>(this IEnumerable<T> enumerable)
        {
            var result = new Queue<T>();
            
            if (enumerable.IsNullOrEmpty())
                return result;

            foreach (var obj in enumerable)
                result.Enqueue(obj);

            return result;
        }
        
        public static Type ParseType<TInterface>(this string input)
            where TInterface : class
        {
            if (string.IsNullOrEmpty(input))
                return null;

            var sourceType = typeof(TInterface);
            var assembly = sourceType.Assembly;
            
            var className = input;
            var typeFullName = $"{sourceType.Namespace}.{className}";
            
            return assembly.GetType(typeFullName);
        }

        public static T[] Copy<T>(this T[] array, Func<T, T> getCopy)
        {
            if (array == null)
                return null;

            var newArray = new T[array.Length];

            for (var i = 0; i < array.Length; i++)
                newArray[i] = getCopy.Invoke(array[i]);

            return newArray;
        }
        
        public static T[] AddItem<T>(this T[] array, T newItem)
        {
            if (array == null)
                return null;

            var newArray = new T[array.Length + 1];

            for (var i = 0; i < array.Length; i++)
                newArray[i] = array[i];

            newArray[newArray.Length - 1] = newItem;

            return newArray;
        }
        
        public static T[] RemoveItem<T>(this T[] array, int index)
        {
            if (array == null || array.Length == 0)
                return null;

            var newArray = new T[array.Length - 1];
            var offset = 0;
            
            for (var i = 0; i < array.Length; i++)
            {
                if (i == index)
                {
                    offset = -1;
                    continue;
                }
                
                newArray[i + offset] = array[i];
            }

            return newArray;
        }

        public static int ToInt32OrDefault(this string value, int defaultInt)
        {
            if (string.IsNullOrEmpty(value) ||
                !int.TryParse(value, out var number))
                return defaultInt;

            return number;
        }

        public static string ToLowerFirstLetter(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;
            
            return input.First().ToString().ToUpper() + String.Join("", input.Skip(1));
        }
        
        public static T ToEnum<T>(this string source, T defaultValue = default(T)) where T : Enum
        {
            if (string.IsNullOrEmpty(source))
                return defaultValue;
            
            try
            {
                return (T) Enum.Parse(typeof(T), source);
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

        // used for conversion enums with the same keys
        public static TResultEnum ConvertEnum<TSourceEnum, TResultEnum>(this TSourceEnum enumValue, TResultEnum defaultValue = default(TResultEnum)) 
            where TSourceEnum : Enum
            where TResultEnum : Enum
        {
            return ToEnum(enumValue.ToString(), defaultValue);
        }

        public static T[] ToEnumArray<T>(params T[] excludeList) where T : Enum
        {
            return Enum.GetNames(typeof(T))
                .Where(typeName => excludeList == null || !excludeList.Contains(typeName.ToEnum<T>()))
                .Select(typeName => typeName.ToEnum<T>())
                .ToArray();
        }

        public static T[] ToEnumArray<T>(int[] indexes, params T[] addToList) where T : Enum
        {
            var result = new List<T>();

            for (var i = 0; i < indexes.Length; i++)
                try
                {
                    var value = (T) Enum.ToObject(typeof(T), indexes[i]);

                    if (value.ToString().Equals(indexes[i].ToString()))
                        continue;

                    result.Add(value);
                }
                catch (Exception)
                {
                }

            if (addToList != null)
                foreach (var value in addToList)
                    result.Add(value);

            return result.ToArray();
        }

        public static void Stop(this Sequence sequence)
        {
            if (sequence == null)
                return;

            sequence.Pause();
            sequence.Kill();
            sequence = null;
        }
        
        
        public static List<T> Shuffle<T>(this List<T> source)
        {
            var sourceCopy = new List<T>(source);

            for (var i = 0; i < source.Count(); i++)
            {
                var randomIndex = UnityEngine.Random.Range(0, sourceCopy.Count);
                source[i] = sourceCopy[randomIndex];
                sourceCopy.RemoveAt(randomIndex);
            }

            return source;
        }
        
        public static HashSet<T> AddRange<T>(this HashSet<T> source, IEnumerable<T> hashSet)
        {
            foreach (var value in hashSet)
                source.Add(value);

            return source;
        }

        public static HashSet<T> RemoveRange<T>(this HashSet<T> source, IEnumerable<T> hashSet)
        {
            foreach (var value in hashSet)
                source.Remove(value);

            return source;
        }

        public static string FirstToUpper(this string source)
        {
            if (string.IsNullOrEmpty(source))
                return source;

            return source.First().ToString().ToUpper() + source.Substring(1);
        }
    }
}