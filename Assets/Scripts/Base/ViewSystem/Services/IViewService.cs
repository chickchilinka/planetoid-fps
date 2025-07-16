using System;
using System.Threading.Tasks;
using ViewSystem.Base;

namespace ViewSystem.Controller
{
    public interface IViewService
    {
        public event Action OnAllViewsClosed;
        void ShowView(Type type);
        
        void ShowView<TView>()
            where TView : class, IView;
        
        void ShowViewOnAllClosed<TView>()
            where TView : class, IView;
        
        void ShowView<TView, TInput>(TInput input)
            where TInput : IViewInput
            where TView : class, IViewWithInput<TInput>;

        void ShowView<TInput>(Type type, TInput input)
            where TInput : IViewInput;

        bool IsViewActive(Type type);
        bool IsViewShowing(Type type);
        bool AllViewsAreClosed(params ViewType[] viewTypes);
        
        TView GetActiveView<TView>()
            where TView : class, IView;
        
        void HideAll();
        void HideAllByType(params ViewType[] viewTypes);
        void HideView(IView view);
        void HideView<TView>()
            where TView : class, IView;
        void HideView(Type type);

        Task CollectViewTypes();
    }
}
