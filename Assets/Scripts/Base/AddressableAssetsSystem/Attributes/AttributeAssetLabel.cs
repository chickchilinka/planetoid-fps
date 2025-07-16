#if UNITY_EDITOR
using System;
using AddressableAssetsSystem.Data;
using UnityEditor;
using UnityEngine;

namespace AddressableAssetsSystem.Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class AttributeAssetLabel : PropertyAttribute
    {
    }

    [CustomPropertyDrawer(typeof(AttributeAssetLabel))]
    public class AttributeAssetLabelProperty : BaseFieldDrawer<AssetLabelConst>
    {
    }
}

#endif