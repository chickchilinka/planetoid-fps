using ViewSystem.Base;
using ViewSystem.Button;
using ViewSystem.Controller;
using Zenject;

namespace ViewSystem.ButtonCommand
{
    public class HideViewButtonCommand : AbstractButton
    {
        private IViewService _viewService;

        private IView _parentView;
        private IView ParentView => _parentView ?? (_parentView = GetComponentInParent<IView>());

        [Inject]
        public void Construct(IViewService viewService)
        {
            _viewService = viewService;
        }

        public override void Activate()
        {
            if (ParentView != null)
            {
                _viewService.HideView(ParentView);
            }
        }

        public void ClearParent()
        {
            _parentView = null;
        }
    }
}
