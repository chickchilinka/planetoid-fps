using System;
using ViewSystem.Animation;

namespace ViewSystem.Base
{
    public abstract class BaseAnimatedView<TViewAnimation> : BaseView, IAnimatedView where TViewAnimation : IViewAnimation
    {
        protected TViewAnimation Animation;

        protected abstract void InitializeAnimation();

        protected virtual void Awake()
        {
            InitializeAnimation();
        }
        
        public override void Show()
        {
            base.Show();
            
            if (Animation != null)
                Animation.AnimateShow(OnEndAnimatingShow);
        }

        public override void Hide(Action onEndHiding)
        {
            if (Animation != null)
                Animation.AnimateHide(() => OnEndAnimatingHide(onEndHiding));   
            else 
                OnEndAnimatingHide(onEndHiding);
        }
        
        protected virtual void OnEndAnimatingShow()
        {
        }
        
        protected virtual void OnEndAnimatingHide(Action onEndHiding)
        {
            base.Hide(onEndHiding);
        }

        protected virtual void OnDestroy()
        {
            Animation?.Clear();
        }
    }
}
