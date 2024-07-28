using System;

namespace ViewSystem.Animation
{
    public interface IViewAnimation
    {
        void AnimateShow(Action callback);
        void AnimateHide(Action callback);
        void Clear();
    }
}
