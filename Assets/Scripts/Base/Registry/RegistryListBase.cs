using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils.Extensions;

namespace Registry
{
    public abstract class RegistryListBase<TData> : ScriptableObject, IRegistryList where TData : class, IRegistryData
    {
        [SerializeField] protected TData[] _registryItems;
        
        public int Length => _registryItems.Length;
        public Type DataType { get; } = typeof(TData);

        public IEnumerator GetEnumerator()
        {
            return _registryItems.GetEnumerator();
        }

        public TData[] GetItems()
        {
            return _registryItems;
        }
        
        public void SetItems(TData[] data)
        {
            _registryItems = data;
        }

        public Dictionary<string, TData> ToDictionary()
        {
            return _registryItems.ToDictionary(key => key.Id, value => value);
        }

        public TData GetById(string id)
        {
            return _registryItems.GetById(id);
        }
    }
}