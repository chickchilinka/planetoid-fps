using System;

namespace ViewSystem.Button.Animation
{
    public interface IButtonAnimation
    {
        void AnimatePress(Action callback);
        void Clear();
    }
}