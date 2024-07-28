using ViewSystem.Animation;

namespace ViewSystem.Base
{
    public abstract class BaseAnimatedViewWithInput<TViewInput, TViewAnimation> : BaseAnimatedView<TViewAnimation>, IAnimatedViewWithInput<TViewInput> 
        where TViewInput : IViewInput 
        where TViewAnimation : IViewAnimation
    {
        public virtual void Show(TViewInput data)
        {
            Show();
        }
    }
}
