using System;

namespace ViewSystem.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple=false, Inherited = true)]
    public class AttributeViewType : Attribute 
    {
        public ViewType ViewType { get; private set; }
        
        public AttributeViewType(ViewType viewType)
        {
            ViewType = viewType;
        }
    }
}
