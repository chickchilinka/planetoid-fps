using ViewSystem;
using ViewSystem.Attributes;
using ViewSystem.Base;

namespace Base.ApplicationMode.View
{
    [AttributeViewType(ViewType.Window)]
    public class InitialWindow : BaseView
    {
        public override ViewLayer ViewLayer => ViewLayer.Window;
    }
}