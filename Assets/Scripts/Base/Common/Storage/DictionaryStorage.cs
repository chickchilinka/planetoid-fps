using System.Collections.Generic;

namespace Base.Common.Storage
{
    public abstract class DictionaryStorage<TKey, TValue>
    {
        protected Dictionary<TKey, TValue> _dictionary = new();

        public IReadOnlyDictionary<TKey, TValue> Dictionary => _dictionary;

        public virtual void Add(TKey key, TValue value)
        {
            _dictionary.TryAdd(key, value);
        }

        public virtual void Remove(TKey key)
        {
            _dictionary.Remove(key);
        }

        public virtual bool ContainsKey(TKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        public virtual bool TryGetValue(TKey key, out TValue value)
        {
            return _dictionary.TryGetValue(key, out value);
        }
    }
}