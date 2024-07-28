namespace ViewSystem.Handler
{
    public class WindowHandler : BaseViewHandler
    {
        public override ViewType ViewType => ViewType.Window;

        public WindowHandler(ViewFactory viewFactory) : base(viewFactory)
        {
        }
    }
}
