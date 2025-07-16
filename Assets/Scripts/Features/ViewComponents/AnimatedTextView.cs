using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Features.ViewComponents
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class AnimatedTextView : MonoBehaviour
    {
        [SerializeField] private float _animationDuration;
        [SerializeField] private TextMeshProUGUI _animatedText;

        private Tween _tween;
        private int _value;
        private bool _isShowWithAnimation;

        public void ChangeValue(int newValue)
        {
            if (!_isShowWithAnimation)
            {
                _value = newValue;
                _animatedText.text = _value.ToString();
                _isShowWithAnimation = true;
            }
            else
            {
                _tween?.Kill();
                _tween = DOTween.To(value => _value = (int)value, _value, newValue, _animationDuration).SetEase(Ease.OutSine);
                _tween.OnUpdate(() => _animatedText.text = _value.ToString());
            }
        }

        private void OnDestroy()
        {
            _tween?.Kill();
        }

    }
}
