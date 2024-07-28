using System;
using DG.Tweening;
using UnityEngine;

namespace ViewSystem.Button.Animation
{
    public class ButtonScaleAnimation : IButtonAnimation
    {
        private readonly Transform _targetTransform;
        
        private Tween _tweenHide;
        
        private const float TagetScale = 1.1f;
        private const float DefaultScale = 1f;

        public ButtonScaleAnimation(Transform targetTransform)
        {
            _targetTransform = targetTransform;
        }

        public void AnimatePress(Action callback)
        {
            Clear();
            
            if (_targetTransform == null)
                return;
            
            _tweenHide = DOTween.Sequence()
                .Append(_targetTransform.DOScale(_targetTransform.localScale, TagetScale).SetEase(Ease.InOutBack))
                .Append(_targetTransform.DOScale(_targetTransform.localScale, DefaultScale).SetEase(Ease.InOutBack))
                .AppendCallback(() => callback?.Invoke());
        }

        public void Clear()
        {
            _targetTransform.localScale = Vector3.one;

            _tweenHide?.Kill();
        }
    }
}