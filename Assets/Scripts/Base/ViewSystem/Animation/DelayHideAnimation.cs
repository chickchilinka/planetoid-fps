using System;
using UniRx;

namespace ViewSystem.Animation
{
    public class DelayHideAnimation : IViewAnimation
    {
        private readonly float _delaySeconds;

        private IDisposable _delayStream;
            
        public DelayHideAnimation(float delaySeconds)
        {
            _delaySeconds = delaySeconds;
        }

        public void AnimateShow(Action callback)
        {
            Clear();
            
            callback?.Invoke();
        }

        public void AnimateHide(Action callback)
        {
            Clear();
            
            _delayStream = Observable.Timer(TimeSpan.FromSeconds(_delaySeconds))
                .Subscribe(_ => callback?.Invoke());
        }

        public void Clear()
        {
            _delayStream?.Dispose();
        }
    }
}
