using System;

namespace ViewSystem.Group
{
    public interface ISelectable
    {
        event Action Trigger;
        bool CanBeSelected { get; }
        void SetSelection(bool isSelected);
    }
}
