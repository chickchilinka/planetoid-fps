using System;
using System.Collections.Generic;
using System.Linq;
using Base;
using DG.Tweening;
using UnityEngine;

namespace Utils.Extensions
{
    public static class GenericExtensions
    {
        public static T GetById<T>(this IEnumerable<T> enumerable, string id) where T : class, IIdentified
        {
            if (string.IsNullOrEmpty(id))
                return null;
            
            foreach (var t in enumerable)
                if (t.Id.Equals(id))
                    return t;

            return null;
        }

        public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable == null)
                return true;
            
            return !enumerable.Any();
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

        public static HashSet<T> AddRange<T>(this HashSet<T> source, IEnumerable<T> hashSet)
        {
            foreach (var value in hashSet)
                source.Add(value);

            return source;
        }
    }
}