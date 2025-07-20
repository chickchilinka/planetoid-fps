using UnityEngine;

namespace Base.Common.Registry
{
    public abstract class RegistryBase<TData> : ScriptableObject, IRegistry
        where TData : class
    {
        [SerializeField] protected TData RegistryData;
        
        public TData Data => RegistryData;
    }
}