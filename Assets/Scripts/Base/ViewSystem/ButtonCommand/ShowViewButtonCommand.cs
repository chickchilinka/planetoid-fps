using System;
using UnityEngine;
using ViewSystem.Attributes;
using ViewSystem.Button;
using ViewSystem.Controller;
using Zenject;

namespace ViewSystem.ButtonCommand
{
    public class ShowViewButtonCommand : AbstractButton
    {
#pragma warning disable 0649
#if UNITY_EDITOR
        [AttributeViewName] 
#endif
        [SerializeField] private string _viewType;
        [SerializeField] private bool _secondTapCloseWindow;
        [SerializeField] private bool _hideAllPreviousWindows = true;
        [SerializeField] private bool _showOnAwake;

        private IViewService _viewService;

        [Inject]
        public void Construct(IViewService viewService)
        {
            _viewService = viewService;
            
            if (_showOnAwake)
                Activate();
        }

        protected override void Awake()
        {
            if (string.IsNullOrEmpty(_viewType))
                Debug.LogError($"[ShowViewButtonCommand] view type {_viewType} can't be null or empty!");
            
            base.Awake();
        }

        public override void Activate()
        {
            if (string.IsNullOrEmpty(_viewType))
            {
                Debug.LogError("[ShowViewButtonCommand] view type {_viewType} can't be null or empty!");
                return;
            }

            var type = Type.GetType(_viewType);

            if (_viewService.IsViewShowing(type))
            {
                if (_secondTapCloseWindow && _hideAllPreviousWindows)
                    _viewService.HideAllByType(ViewType.Window);
                else if (_secondTapCloseWindow)
                    _viewService.HideView(type);
            }
            else
            {
                if (_hideAllPreviousWindows)
                    _viewService.HideAllByType(ViewType.Window);
                
                _viewService.ShowView(type);
            }
        }
    }
}
