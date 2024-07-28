namespace ViewSystem.Base
{
    public class BaseHintViewWithInput<TViewInput> : BaseHintView, IAnimatedViewWithInput<TViewInput> 
        where TViewInput : IViewInput 
    {
        public virtual void Show(TViewInput data)
        {
            Show();
        }
    }
}
