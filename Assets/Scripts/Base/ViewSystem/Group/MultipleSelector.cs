using System;
using System.Collections.Generic;
using System.Linq;

namespace ViewSystem.Group
{
    public class MultipleSelector<TSelectable> : ISelector<TSelectable>
        where TSelectable : class, ISelectable
    {
        private readonly List<TSelectable> _items;
        private readonly List<TSelectable> _selectedItems = new List<TSelectable>();

        public event Action<TSelectable> SelectionChanged;

        public MultipleSelector(IEnumerable<TSelectable> items, Func<TSelectable, bool> isSelected)
        {
            _items = items.ToList();

            for (int i = 0; i < _items.Count; i++)
            {
                var i1 = i;
                _items[i].Trigger += () => Select(i1);
                _items[i].SetSelection(isSelected?.Invoke(_items[i]) ?? false);
            }
        }

		private void Select(int index)
        {
			var selectedItem = _items[index];

			if (_selectedItems.Contains(selectedItem))
            {
				Deselect(index);
				return;
			}
			
			if (!_items[index].CanBeSelected)
				return;

			_items[index].SetSelection(true);
			_selectedItems.Add(_items[index]);
			
            SelectionChanged?.Invoke(_items[index]);
        }

		private void Deselect(int index)
        {
			if (!_selectedItems.Contains(_items[index]))
				return;

			_items[index].SetSelection(false);
			_selectedItems.Remove(_items[index]);

            SelectionChanged?.Invoke(_items[index]);
		}

		public void Dispose()
        {
            SelectionChanged = delegate { };
        }
    }
}

