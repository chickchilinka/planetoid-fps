using ViewSystem.Button;
using ViewSystem.Controller;
using Zenject;

namespace ViewSystem.ButtonCommand
{
    public class HideAllHintsButtonCommand : AbstractButton
    {
        private IViewController _viewController;
        
        [Inject]
        public void Construct(IViewController viewController)
        {
            _viewController = viewController;
        }

        public override void Activate()
        {
            _viewController.HideAllByType(ViewType.Hint);
        }
    }
}
