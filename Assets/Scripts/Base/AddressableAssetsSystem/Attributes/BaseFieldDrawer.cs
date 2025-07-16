using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AddressableAssetsSystem.Attributes
{
#if UNITY_EDITOR
    public abstract class BaseFieldDrawer<T> : PropertyDrawer 
    {
        private string[] _fieldListNames;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        { 
            if (_fieldListNames == null)
                BuildTypeList();

            var selectedIndex = Array.FindIndex(_fieldListNames, match => match.Equals(property.stringValue));
            
            if (selectedIndex == -1)
                selectedIndex = 0;

            selectedIndex = EditorGUI.Popup(position, label.text, selectedIndex, _fieldListNames);
            
            if (_fieldListNames.Length > 0)
                property.stringValue = _fieldListNames[selectedIndex];
        } 
      
        private void BuildTypeList()
        {
            _fieldListNames = typeof(T).GetFields().Select(x => x.Name).ToArray(); 
        }
    }
#endif
}