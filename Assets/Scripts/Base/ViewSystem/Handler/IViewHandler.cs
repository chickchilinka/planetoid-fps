using System;
using System.Collections.Generic;
using ViewSystem.Base;

namespace ViewSystem.Handler
{
    public interface IViewHandler
    {
        ViewType ViewType { get; }
        bool AllViewsAreClosed { get; }
        
        bool IsWindowShowing(Type type);
        
        Type GetActiveView();
        TView GetActiveView<TView>() where TView : class, IView;
        
        bool ShowView(Type type);
        
        bool ShowViewWithInput<TView, TInput>(TInput input)
            where TInput : IViewInput
            where TView : class, IViewWithInput<TInput>;

        bool ShowViewWithInput<TInput>(Type type, TInput input)
            where TInput : IViewInput;
        
        List<string> HideAllViews();
        
        bool HideView(Type type);
    }
}
