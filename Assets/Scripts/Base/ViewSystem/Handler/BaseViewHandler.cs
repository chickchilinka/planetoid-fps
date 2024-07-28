using System;
using System.Collections.Generic;
using UnityEngine;
using ViewSystem.Base;

namespace ViewSystem.Handler
{
    public abstract class BaseViewHandler : IViewHandler
    {
        private readonly ViewFactory _viewFactory;
        
        private IView _activeView;
        
        public abstract ViewType ViewType { get; }
        
        public bool AllViewsAreClosed => _activeView == null || _activeView.Equals(null);

        protected BaseViewHandler(ViewFactory viewFactory)
        {
            _viewFactory = viewFactory;
        }

        public bool IsWindowShowing(Type type)
        {
            return _activeView != null && _activeView.GetType() == type ||
                   !AllViewsAreClosed && _viewFactory.IsViewActivated(type);
        }

        public Type GetActiveView()
        {
            return _activeView?.GetType();
        }

        public TView GetActiveView<TView>() where TView : class, IView
        {
            return _activeView as TView;
        }

        public bool ShowView(Type type)
        {
            if (!CanActivateView(type)) 
                return false;
            
            var view = _viewFactory.GetView(type);

            ActivateView(view);
            view.Show();
            Log("Shown", type);

            return true;
        }

        public bool ShowViewWithInput<TView, TInput>(TInput input)
            where TInput : IViewInput
            where TView : class, IViewWithInput<TInput>
        {
            return ShowViewWithInput(typeof(TView), input);
        }

        public bool ShowViewWithInput<TInput>(Type type, TInput input) where TInput : IViewInput
        {
            if (!CanActivateView(type)) 
                return false;

            var view = _viewFactory.GetView(type) as IViewWithInput<TInput>;

            if (view == null)
            {
                Debug.LogError($"View {type.Name} doesn't have input");
                return false;
            }

            ActivateView(view);
            view.Show(input);
            Log("Shown", type);

            return true;
        }

        private string HideActiveView()
        {
            if (_activeView == null)
                return String.Empty;

            DeactivateView(_activeView);
            
            var hiddenViewName = _activeView.GetType().Name;
            
            Log("Hidden", _activeView.GetType());

            if (_viewFactory.ActiveViewsCount <= 0)
            {
                ActivateView(null);
                
                return hiddenViewName;
            }
            
            while (_viewFactory.ActiveViewsCount > 0)
            {
                var lastActiveView = _viewFactory.GetLastOrDefaultActiveView();

                if (lastActiveView == null || lastActiveView.Equals(null))
                {
                    _viewFactory.DeactivateView(lastActiveView);
                    
                    continue;
                }
                
                break;
            }
            
            ActivateView(_viewFactory.GetLastOrDefaultActiveView());
            
            return hiddenViewName;
        }
        
        public List<string> HideAllViews()
        {
            var hiddenViewNames = new List<string>();
            
            while (_activeView != null)
            {
                var hiddenViewName = HideActiveView();
                
                if (!string.IsNullOrEmpty(hiddenViewName))
                    hiddenViewNames.Add(hiddenViewName);
            }

            return hiddenViewNames;
        }

        public void HideView(IView view)
        {
            HideView(view.GetType());
        }

        public bool HideView(Type type)
        {
            if (_activeView == null)
                return false;
            
            if (type == _activeView.GetType())
                HideActiveView();
            else
            {
                var view = _viewFactory.GetActivatedView(type);

                if (view != null)
                {
                    Log("Hidden", view.GetType());
                    DeactivateView(view);
                }
                else 
                    Log("Hidden failed for " + type.Name);
            }
            
            return true;
        }
        
        private bool CanActivateView(Type type)
        {
            var canActivate = type != null && (_activeView == null || _activeView.GetType() != type || !_activeView.IsShown);
            
            if (!canActivate)
                Log("can't activate view", type);
            
            return canActivate;
        }

        private void DeactivateView(IView view)
        {
            view?.Hide(null);
            _viewFactory.DeactivateView(view);
        }
        
        private void ActivateView(IView view)
        {
            _activeView = view;

            if (_viewFactory.GetLastOrDefaultActiveView() != _activeView)
                _viewFactory.ActivateView(_activeView);
        }
   
        private void Log(string message, Type type = null)
        {
//            Debug.LogWarning($"[{ViewType}] {message} {(type != null ? type.Name : "---")}");
        }
    }
}
