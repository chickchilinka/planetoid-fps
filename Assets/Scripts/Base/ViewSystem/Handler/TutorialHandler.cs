namespace ViewSystem.Handler
{
    public class TutorialHandler : BaseViewHandler
    {
        public override ViewType ViewType => ViewType.Tutorial;

        public TutorialHandler(ViewFactory viewFactory) : base(viewFactory)
        {
        }
    }
}
