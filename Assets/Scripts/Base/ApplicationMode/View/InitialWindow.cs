using ViewSystem;
using ViewSystem.Attributes;
using ViewSystem.Base;

namespace General.View
{
    [AttributeViewType(ViewType.Window)]
    public class InitialWindow : BaseView
    {
        public override ViewLayer ViewLayer => ViewLayer.Window;
    }
}