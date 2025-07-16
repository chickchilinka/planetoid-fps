using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils.Debugger;
using Utils.Extensions;
using Random = System.Random;

namespace Utils.Services
{
    public class RandomService
    {
        private Random _random;
        private int _seed;
        
        public int Seed => _seed;

        public void Initialize(int seed = Const.Values.DefaultSeed)
        {
            if (seed == Const.Values.DefaultSeed)
                seed = (int) DateTime.Now.Ticks;
            seed = -2004336040;
            _seed = seed;
            _random = new Random(_seed);

            PrintLog.Info(LogTag.Random, $"initialized with seed {_seed}");
        }

        public int GetRandomIntValue()
        {
            return _random.Next();
        }
        
        public int GetRandomIntValue(int min, int max)
        {
            if (min == max)
                Debug.LogWarning("[Random] min == max, returned min");

            return _random.Next(min, max);
        }

        private float GetRandomFloatValue()
        {
            return (float) _random.NextDouble();
        }

        public float GetRandomFloatValue01()
        {
            var value = GetRandomFloatValue();
            return value - (int) value;
        }

        public int GetRandomIndexByInt(int[] weights)
        {
            if (weights.Length == 1)
                return 0;

            var total = weights.Sum();

            if (total == 0)
                return 0;

            var randomValue = GetRandomIntValue(0, total);
            var from = 0;

            for (var i = 1; i <= weights.Length; i++)
            {
                var to = i == weights.Length ? total : from + weights[i - 1];

                if (randomValue >= from && randomValue <= to)
                    return i - 1;

                from += i == weights.Length ? 0 : weights[i - 1];
            }

            Debug.LogWarning($"GetRandomIndexByInt error with random value {randomValue} and weights {weights.Log()}");
            return GetRandomIntValue(0, weights.Length);
        }
        
        public int GetRandomIndexByFloat(float[] weights)
        {
            if (weights.Length == 1)
                return 0;

            var total = weights.Sum();

            if (Math.Abs(total) < Const.Values.Threshold)
                return 0;

            var randomValue = GetRandomIntValue(0, (int)total);
            var from = 0f;

            for (var i = 1; i <= weights.Length; i++)
            {
                var to = i == weights.Length ? total : from + weights[i - 1];

                if (randomValue >= from && randomValue <= to)
                    return i - 1;

                from += i == weights.Length ? 0f : weights[i - 1];
            }

            Debug.LogWarning($"GetRandomIndexByFloat error with random value {randomValue} and weights {weights.Log()}");
            return GetRandomIntValue(0, weights.Length);
        }

        public int GetRandomIndex<T>(IEnumerable<T> source, List<T> exceptList = null)
        {
            var sourceList = new List<T>(source);
            var copyWithoutExceptList = new List<T>(sourceList.Count);

            for (var i = 0; i < sourceList.Count; i++)
            {
                var sourceElement = sourceList[i];
                
                if (exceptList != null && exceptList.Contains(sourceElement))
                    continue;

                copyWithoutExceptList.Add(sourceElement);
            }

            if (copyWithoutExceptList.IsNullOrEmpty())
                return Const.Values.InvalidIndex;

            var randomIndex = GetRandomIntValue(0, copyWithoutExceptList.Count);

            return sourceList.FindIndex(t => t.Equals(copyWithoutExceptList[randomIndex]));
        }

        public List<T> Shuffle<T>(List<T> source)
        {
            var sourceCopy = new List<T>(source);
            var randomIndex = 0;

            for (var i = 0; i < source.Count; i++)
            {
                randomIndex = GetRandomIntValue(0, sourceCopy.Count);
                source[i] = sourceCopy[randomIndex];
                sourceCopy.RemoveAt(randomIndex);
            }

            return source;
        }
    }
}