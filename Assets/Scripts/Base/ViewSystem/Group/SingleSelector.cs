using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ViewSystem.Group
{
    public class SingleSelector<TSelectable> : ISelector<TSelectable>
        where TSelectable : class, ISelectable
    {
        private readonly List<TSelectable> _items;
        private readonly bool _canBeUnselected;

        private const int DeselectedIndex = -1;

        private int _index = DeselectedIndex;
        private TSelectable _item;
        public TSelectable SelectedItem => _item;

        public event Action<TSelectable> SelectionChanged = delegate {  };

        public SingleSelector(IEnumerable<TSelectable> items, bool canBeUnselected)
        {
            _items = items.ToList();
            _canBeUnselected = canBeUnselected;

            for (int i = 0; i < _items.Count; i++)
            {
                var i1 = i;
                _items[i].Trigger += () => Select(i1);
                _items[i].SetSelection(false);
            }

            if (!canBeUnselected)
                SelectFirstAvailableItem();
        }
        
        public void Select(int index)
        {
            if (index < 0 || index >= _items.Count)
            {
                Debug.Log($"Index {index} out of range {_items.Count}");
                return;
            }
            
            if (!_items[index].CanBeSelected)
                return;
            
            var isSelfSelection = index == _index;
            
            if (!isSelfSelection || _canBeUnselected)
                DeselectCurrent();

            if (isSelfSelection)
            {
                if (_index == DeselectedIndex)
                    SelectionChanged?.Invoke(null);
                
                return;
            }
            
            _item = _items[index];
            _item.SetSelection(true);
            _index = index;

            SelectionChanged?.Invoke(_item);
        }
        
        public void DeselectCurrent()
        {
            if (_item == null)
                return;
            
            _item.SetSelection(false);
            _item = null;
            _index = DeselectedIndex;
        }

        public void SelectFirstAvailableItem()
        {
            DeselectCurrent();

            for (var i = 0; i < _items.Count; i++)
            {
                if (!_items[i].CanBeSelected)
                    continue;
                
                Select(i);
                return;
            }
        }

        public void UpdateSelection()
        {
            for (var i = 0; i < _items.Count; i++)
                _items[i].SetSelection(_index == i);
        }

        public void Dispose()
        {
            SelectionChanged = delegate { };
        }
    }
}
