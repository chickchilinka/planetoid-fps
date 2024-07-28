using System;
using DG.Tweening;
using UnityEngine;

namespace ViewSystem.Animation
{
    public class VerticalJumpAnimation : IViewAnimation
    {
        public bool IsHiding => _tweenHide != null && _tweenHide.IsActive() && _tweenHide.IsPlaying();

        private readonly CanvasGroup _canvasGroup;
        private readonly RectTransform _panelRectTransform;
        
        private const float ShowFade = 1f;
        private const float HideFade = 0f;
        private const float HeightMultiplier = 0.35f;
        private const float ShowingTime = 0.3f;
        private const float HidingTime = 0.2f;
        private const float Overshoot = 2f;

        private readonly Vector2 _startScale = new Vector2(0.8f, 0.8f);
        private readonly float _defaultY;

        private float _tagetY = 0f;

        private Tween _tweenHide;
        private Tween _tweenShow;
        
        public VerticalJumpAnimation(CanvasGroup canvasGroup, RectTransform panelRectTransform)
        {
            _canvasGroup = canvasGroup;
            _panelRectTransform = panelRectTransform;

            _defaultY = -_panelRectTransform.rect.height * HeightMultiplier;
            _tagetY = _panelRectTransform.localPosition.y;
        }
    
        public void AnimateShow(Action callback)
        {
            Clear();

            _panelRectTransform.anchoredPosition3D = new Vector3(_panelRectTransform.anchoredPosition.x, _defaultY, 0);

            _panelRectTransform.localScale = Vector2.one * _startScale;

            _tweenShow = DOTween.Sequence()
                .Append(_canvasGroup?.DOFade(ShowFade, ShowingTime).SetEase(Ease.OutCirc))
                .Join(_panelRectTransform.DOLocalMoveY(_tagetY, ShowingTime).SetEase(Ease.OutSine))
                .Join(_panelRectTransform.DOScale(Vector2.one, ShowingTime).SetEase(Ease.OutBack, 2f))
                .AppendCallback(() => callback?.Invoke());
        }

        public void AnimateHide(Action callback)
        {
            Clear();
            
            _tweenHide = DOTween.Sequence()
                .Append(_canvasGroup?.DOFade(HideFade, HidingTime).SetEase(Ease.InQuart))
                .Join(_panelRectTransform.DOLocalMoveY(_defaultY, HidingTime).SetEase(Ease.InBack, Overshoot))
                .AppendCallback(() => callback?.Invoke());
        }

        public void Clear()
        {
            _tweenHide?.Kill();
            _tweenShow?.Kill();
        }
    }
}