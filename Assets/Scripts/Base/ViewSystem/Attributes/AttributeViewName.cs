
#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using ViewSystem.Base;

namespace ViewSystem.Attributes
{
    public class AttributeViewName : PropertyAttribute
    {
    }

    [CustomPropertyDrawer(typeof(AttributeViewName))]
    public class ViewNameProperty : BasePropertyDrawer<IView>
    {
    }
}

#endif