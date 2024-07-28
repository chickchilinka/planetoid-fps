using System;

namespace ViewSystem.Group
{
    public interface ISelector<TSelectable> 
        where TSelectable : class, ISelectable
    {
        event Action<TSelectable> SelectionChanged;
    }
}
