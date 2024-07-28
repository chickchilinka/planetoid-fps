using System;
using DG.Tweening;
using UnityEngine;

namespace ViewSystem.Animation
{
    public class FadeAndScaleViewAnimation : IViewAnimation
    {
        public bool IsHiding => _tweenHide != null && _tweenHide.IsActive() && _tweenHide.IsPlaying();

        private readonly CanvasGroup _canvasGroup;
        private readonly RectTransform _panelRectTransform;
        
        private const float Scale = 0.95f;
        private const float ShowFade = 1f;
        private const float HideFade = 0f;
        private const float TagetScale = 1f;
        private const float ShowingTime = 0.5f;
        private const float HidingTime = 0.5f;
        private const float Overshoot = 2f;

        private Tween _tweenHide;
        private Tween _tweenShow;
        
        public FadeAndScaleViewAnimation(CanvasGroup canvasGroup, RectTransform panelRectTransform)
        {
            _canvasGroup = canvasGroup;
            _panelRectTransform = panelRectTransform;
        }
    
        public void AnimateShow(Action callback)
        {
            Clear();

            _panelRectTransform.localScale = Vector3.one * Scale;
            _tweenShow = DOTween.Sequence()
                .Append(_canvasGroup.DOFade(ShowFade, ShowingTime).SetEase(Ease.OutCirc))
                .Join(_panelRectTransform.DOScale(TagetScale, ShowingTime).SetEase(Ease.InOutBack))
                .AppendCallback(() => callback?.Invoke());
        }

        public void AnimateHide(Action callback)
        {
            Clear();
            
            _tweenHide = DOTween.Sequence()
                .Append(_canvasGroup.DOFade(HideFade, HidingTime).SetEase(Ease.InQuart))
                .Join(_panelRectTransform.DOScale(Scale, HidingTime).SetEase(Ease.InBack, Overshoot))
                .AppendCallback(() => callback?.Invoke());
        }

        public void Clear()
        {
            _tweenHide?.Kill();
            _tweenShow?.Kill();
        }
    }
}
