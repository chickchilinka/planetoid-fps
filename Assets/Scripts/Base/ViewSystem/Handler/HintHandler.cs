namespace ViewSystem.Handler
{
    public class HintHandler : BaseViewHandler
    {
        public override ViewType ViewType => ViewType.Hint;

        public HintHandler(ViewFactory viewFactory) : base(viewFactory)
        {
        }
    }
}
