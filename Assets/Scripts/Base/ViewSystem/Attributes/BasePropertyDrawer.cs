#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ViewSystem.Attributes
{
    public abstract class BasePropertyDrawer<T> : PropertyDrawer 
    {
        private Type[] _typeList;
        private string[] _typeListNames;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        { 
            if (_typeList == null)
                BuildTypeList();

            var selectedIndex = Array.FindIndex(_typeList, match => match.FullName.Equals(property.stringValue));
            
            if (selectedIndex == -1)
                selectedIndex = 0;

            selectedIndex = EditorGUI.Popup(position, label.text, selectedIndex, _typeListNames);
            property.stringValue = _typeList[selectedIndex].FullName;
        } 

        public IEnumerable<Type> FindDerivedTypes(Assembly assembly, Type baseType)
        {
            return assembly.GetTypes()
                .Where(t => t != baseType && baseType.IsAssignableFrom(t));
        } 
        
        private void BuildTypeList()
        {
            _typeList = FindDerivedTypes(typeof(T).Assembly, typeof(T)).ToArray();
            Array.Sort(_typeList, (one,two) => string.CompareOrdinal(one.Name, two.Name));
            _typeListNames = new string[_typeList.Length];
            
            for (int i = 0; i < _typeList.Length; i ++)
                _typeListNames[i] = _typeList[i].Name;
        }
    }
}
#endif