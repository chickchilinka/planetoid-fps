using System.Collections.Generic;

namespace Base.Common.Storage
{
    public class Storage<T>
    {
        protected List<T> _items = new();

        public IReadOnlyList<T> Items => _items;

        public virtual void Add(T item)
        {
            _items.Add(item);
        }

        public virtual void Remove(T item)
        {
            _items.Remove(item);
        }
    }
}