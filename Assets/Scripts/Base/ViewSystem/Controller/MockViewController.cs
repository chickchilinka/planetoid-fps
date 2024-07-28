using System;
using System.Threading.Tasks;
using ViewSystem.Base;
using Zenject;

namespace ViewSystem.Controller
{
    public class MockViewController : IViewController
    {
        private readonly SignalBus _signalBus;

        public MockViewController(SignalBus signalBus)
        {
            _signalBus = signalBus;
        }

        public event Action OnAllViewsClosed;

        public void ShowView(Type type)
        {
            ShowView(type.Name);
        }

        public void ShowView<TView>() 
            where TView : class, IView
        {
            ShowView(typeof(TView));
        }

        public void ShowViewOnAllClosed<TView>() where TView : class, IView
        {
            ShowView<TView>();
        }

        public void ShowView<TView, TInput>(TInput input) 
            where TView : class, IViewWithInput<TInput> 
            where TInput : IViewInput
        {
            ShowView(typeof(TView));
        }

        public void ShowView<TInput>(Type type, TInput input) where TInput : IViewInput
        {
            ShowView(type);
        }

        public bool IsViewActive(Type type)
        {
            return false;
        }

        public bool IsViewShowing(Type type)
        {
            return false;
        }

        public bool AllViewsAreClosed(params ViewType[] viewTypes)
        {
            return true;
        }

        public TView GetActiveView<TView>() 
            where TView : class, IView
        {
            return null;
        }

        public void HideAll()
        {
            HideView(String.Empty);
        }

        public void HideAllByType(params ViewType[] viewTypes)
        {
            HideView(String.Empty);
        }

        public void HideAllViews()
        {
            HideView(String.Empty);
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
            HideView(type.Name);
        }

        public async Task CollectViewTypes()
        {
            
        }

        private void ShowView(string name)
        {
            _signalBus.TryFire(new ViewSignals.Shown(name));
        }
        
        private void HideView(string name)
        {
            _signalBus.TryFire(new ViewSignals.Hidden(name, true));
        }
    }
}
