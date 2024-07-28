
namespace ViewSystem.Base
{
    public abstract class BaseViewWithInput<TViewInput> : BaseView, IViewWithInput<TViewInput> where TViewInput : IViewInput
    {
        public virtual void Show(TViewInput data)
        {
            Show();
        }
    }
}
