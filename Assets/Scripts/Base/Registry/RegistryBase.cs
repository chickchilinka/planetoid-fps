using UnityEngine;

namespace Registry
{
    public abstract class RegistryBase<TData> : ScriptableObject, IRegistryClass
        where TData : class
    {
        [SerializeField] protected TData RegistryData;
        
        public TData Data => RegistryData;
    }
}