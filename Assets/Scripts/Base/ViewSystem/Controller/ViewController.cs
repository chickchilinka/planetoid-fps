using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Utils.Debugger;
using ViewSystem.Attributes;
using ViewSystem.Base;
using ViewSystem.Handler;
using Zenject;

namespace ViewSystem.Controller
{
    public class ViewController : IViewController
    {
        private readonly SignalBus _signalBus;
        private readonly Dictionary<ViewType, IViewHandler> _handlers;

        private readonly List<Type> _viewsQueueToShow = new List<Type>();
        private readonly Dictionary<Type, ViewType> _typeToViewTypes = new Dictionary<Type, ViewType>();
        private event Action _onAllViewsClosed; 
        public event Action OnAllViewsClosed
        {
            add => _onAllViewsClosed += value;
            remove => _onAllViewsClosed -= value;
        }

        public ViewController(SignalBus signalBus, DiContainer diContainer)
        {
            _signalBus = signalBus;
            _handlers = diContainer.ResolveAll<IViewHandler>()
                .ToDictionary(h => h.ViewType, h => h);
        }

        public void ShowView(Type type)
        {
            var handler = GetHandler(type);
            var result = handler?.ShowView(type);

            FireSignal(result, new ViewSignals.Shown(type.Name));
        }

        public void ShowView<TView>() where TView : class, IView
        {
            ShowView(typeof(TView));
        }

        public void ShowViewOnAllClosed<TView>() where TView : class, IView
        {
            ShowViewOnAllClosed(typeof(TView));
        }

        public void ShowView<TView, TInput>(TInput input) where TView : class, IViewWithInput<TInput>
            where TInput : IViewInput
        {
            ShowView(typeof(TView), input);
        }

        public void ShowView<TInput>(Type type, TInput input) where TInput : IViewInput
        {
            var result = GetHandler(type)?.ShowViewWithInput(type, input);
            FireSignal(result, new ViewSignals.Shown(type.Name));
        }

        public bool IsViewShowing(Type type)
        {
            var handler = GetHandler(type);
            if (handler == null)
                return false;

            return handler.IsWindowShowing(type);
        }

        public bool AllViewsAreClosed()
        {
            return _handlers.Values.All(handler => handler.AllViewsAreClosed);
        }
        public bool AllViewsAreClosed(params ViewType[] viewTypes)
        {
            foreach (var viewType in viewTypes)
            {
                var handler = GetHandler(viewType);

                if (handler != null && !handler.AllViewsAreClosed)
                    return false;
            }

            return true;
        }

        public bool IsViewActive(Type type)
        {
            var activeView = GetHandler(type)?.GetActiveView();

            return activeView != null && activeView == type;
        }

        public TView GetActiveView<TView>() where TView : class, IView
        {
            return GetHandler(typeof(TView))?.GetActiveView<TView>();
        }

        public void HideAll()
        {
            HideAllByType(ViewType.Hint, ViewType.Window);
        }

        public void HideAllByType(params ViewType[] viewTypes)
        {
            var closedViewNames = new List<string>();

            foreach (var viewType in viewTypes)
            {
                var handler = GetHandler(viewType);

                if (handler == null)
                    continue;

                var viewNames = handler.HideAllViews();
                closedViewNames.AddRange(viewNames);
            }

            for (var i = 0; i < closedViewNames.Count; i++)
            {
                var viewName = closedViewNames[i];

                FireSignal(true, new ViewSignals.Hidden(viewName, i == closedViewNames.Count - 1));
            }
            
            CheckViewsInQueue();
        }

        public void HideView(IView view)
        {
            HideView(view.GetType());
        }

        public void HideView<TView>() where TView : class, IView
        {
            HideView(typeof(TView));
        }

        public void HideView(Type type)
        {
            var handler = GetHandler(type);
            var result = handler?.HideView(type);

            FireSignal(result, new ViewSignals.Hidden(type.Name, AllViewsAreClosed(ViewType.Window)));
            
            CheckViewsInQueue();
        }

        private void CheckViewsInQueue()
        {
            if (_viewsQueueToShow.Count == 0)
            {
                if(AllViewsAreClosed())
                    _onAllViewsClosed?.Invoke();
                return;
            }

            var viewType = _viewsQueueToShow[0];

            ShowViewOnAllClosed(viewType);
        }

        private void ShowViewOnAllClosed(Type type)
        {
            var viewType = GetViewType(type);

            _viewsQueueToShow.Remove(type);

            if (AllViewsAreClosed(viewType, ViewType.Tutorial))
                ShowView(type);
            else
                _viewsQueueToShow.Add(type);
        }

        private void FireSignal<TSignal>(bool? result, TSignal signal) where TSignal : ViewSignals.BaseActivitySignal
        {
            if (result == null || !result.Value)
                return;

            _signalBus.Fire(signal);
        }

        private IViewHandler GetHandler(Type type)
        {
            return GetHandler(GetViewType(type));
        }

        private ViewType GetViewType(Type type)
        {
            if (!_typeToViewTypes.ContainsKey(type))
                return ViewType.Window;
            return _typeToViewTypes[type];
        }

        private IViewHandler GetHandler(ViewType viewType)
        {
            if (!_handlers.ContainsKey(viewType))
            {
                PrintLog.Error(LogTag.View, $"There is no hadler for <{viewType}>!");
                return null;
            }

            return _handlers[viewType];
        }

        public async Task CollectViewTypes()
        {
            await Task.Yield();
            var baseType = typeof(IView);
            var types = GetType().Assembly.GetTypes()
                .Where(p => p != baseType && baseType.IsAssignableFrom(p));
            foreach (var type in types)
            {
                var attribute = (AttributeViewType)type.GetCustomAttribute(typeof(AttributeViewType));
                if (attribute == null)
                {
                    continue;
                }

                var viewType = attribute.ViewType;
                _typeToViewTypes.Add(type, viewType);
            }
        }
    }
}