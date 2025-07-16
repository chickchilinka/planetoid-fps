using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Features.ViewComponents
{
    [RequireComponent(typeof(Slider))]
    public class AnimatedSliderView : MonoBehaviour
    {
        [SerializeField] private float _animationDuration;
        [SerializeField] private Slider _animatedSlider;

        private Tween _tween;
        private float _value;
        private bool _isShowWithAnimation;

        public void ChangeValue(float newValue, bool force = false)
        {
            _tween?.Kill();

            if (!_isShowWithAnimation || force)
            {
                _value = newValue;
                _animatedSlider.value = _value;
                _isShowWithAnimation = true;
            }
            else
            {
                _tween = _animatedSlider.DOValue(newValue, _animationDuration).SetEase(Ease.OutSine);
            }
        }

        private void OnDestroy()
        {
            _tween?.Kill();
        }

    }
}
