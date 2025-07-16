using ViewSystem.Button;
using ViewSystem.Controller;
using Zenject;

namespace ViewSystem.ButtonCommand
{
    public class HideAllHintsButtonCommand : AbstractButton
    {
        private IViewService _viewService;
        
        [Inject]
        public void Construct(IViewService viewService)
        {
            _viewService = viewService;
        }

        public override void Activate()
        {
            _viewService.HideAllByType(ViewType.Hint);
        }
    }
}
