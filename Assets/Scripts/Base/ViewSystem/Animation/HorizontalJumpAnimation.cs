
using System;
using DG.Tweening;
using UnityEngine;

namespace ViewSystem.Animation
{
    public class HorizontalJumpAnimation : IViewAnimation
    {
        public bool IsHiding => _tweenHide != null && _tweenHide.IsActive() && _tweenHide.IsPlaying();

        private readonly CanvasGroup _canvasGroup;
        private readonly RectTransform _panelRectTransform;
        
        private const float ShowFade = 1f;
        private const float HideFade = 0f;
        private const float HeightMultiplier = 1f;
        private const float ShowingTime = 0.3f;
        private const float HidingTime = 0.2f;
        private const float Overshoot = 2f;

        private readonly Vector2 _startScale = new Vector2(1f, 1f);
        private readonly float _defaultX;

        private float _tagetX = 0f;

        private Tween _tweenHide;
        private Tween _tweenShow;
        
        public HorizontalJumpAnimation(CanvasGroup canvasGroup, RectTransform panelRectTransform)
        {
            _canvasGroup = canvasGroup;
            _panelRectTransform = panelRectTransform;
            _defaultX = -Screen.width * HeightMultiplier;
            _tagetX = _panelRectTransform.localPosition.x;
        }
    
        public void AnimateShow(Action callback)
        {
            Clear();

            _panelRectTransform.anchoredPosition3D = new Vector3(_defaultX, _panelRectTransform.anchoredPosition.y, 0);

            _panelRectTransform.localScale = Vector2.one * _startScale;

            _tweenShow = DOTween.Sequence()
                .Append(_panelRectTransform.DOLocalMoveX(_tagetX, ShowingTime).SetEase(Ease.OutSine))
                .Join(_panelRectTransform.DOScale(Vector2.one, ShowingTime).SetEase(Ease.OutBack, 2f))
                .AppendCallback(() => callback?.Invoke());
        }

        public void AnimateHide(Action callback)
        {
            Clear();
            
            _tweenHide = DOTween.Sequence()
                .Join(_panelRectTransform.DOLocalMoveX(_defaultX, HidingTime).SetEase(Ease.InBack, Overshoot))
                .AppendCallback(() => callback?.Invoke());
        }

        public void Clear()
        {
            _tweenHide?.Kill();
            _tweenShow?.Kill();
        }
    }
}