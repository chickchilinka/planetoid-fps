using UnityEngine;
using ViewSystem.Base;
using ViewSystem.Button;
using ViewSystem.Controller;
using Zenject;

namespace ViewSystem.ButtonCommand
{
    public abstract class AbstractShowViewWithInputButtonCommand<TView, TViewInput> : AbstractButton
        where TView : class, IViewWithInput<TViewInput>
        where TViewInput : IViewInput
    {
        private IViewService _viewService;

        private TViewInput _viewInput;

        [Inject]
        public void Construct(IViewService viewService)
        {
            _viewService = viewService;
        }

        public void BindData(TViewInput viewInput)
        {
            _viewInput = viewInput;
        }

        public override void Activate()
        {
            if (_viewInput == null)
            {
                Debug.LogError($"View input is null. Use BindData method to initialize data for view! {GetType().Name}");
                return;
            }
            
            if (!_viewService.IsViewShowing(typeof(TView)))
                _viewService.ShowView<TView, TViewInput>(_viewInput);
        }
    }
}