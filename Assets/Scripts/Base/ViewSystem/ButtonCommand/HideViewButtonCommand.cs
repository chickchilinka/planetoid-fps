using ViewSystem.Base;
using ViewSystem.Button;
using ViewSystem.Controller;
using Zenject;

namespace ViewSystem.ButtonCommand
{
    public class HideViewButtonCommand : AbstractButton
    {
        private IViewController _viewController;

        private IView _parentView;
        private IView ParentView => _parentView ?? (_parentView = GetComponentInParent<IView>());

        [Inject]
        public void Construct(IViewController viewController)
        {
            _viewController = viewController;
        }

        public override void Activate()
        {
            if (ParentView != null)
            {
                _viewController.HideView(ParentView);
            }
        }

        public void ClearParent()
        {
            _parentView = null;
        }
    }
}
