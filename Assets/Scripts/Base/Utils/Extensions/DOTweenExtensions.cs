using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Utils.Extensions
{
    public static class DOTweenExtensions
    {
//        public static void DOTweenInt(this Text labelField, int startValue, int endValue, float speed)
//        {
//            DOTween.Kill(labelField);
//            DOTween.To(() => startValue, value => labelField.text = value.ToString("N0"), endValue, speed);
//        }
//
//        public static void DOTweenInt(this Text labelField, int endValue, float speed)
//        {
//            int startValue;
//            if (!int.TryParse(labelField.text, out startValue)) startValue = 0;
//            DOTweenInt(labelField, startValue, endValue, speed);
//        }
//        
//        public static void DOTweenLong(this Text labelField, long endValue, float speed)
//        {
//            int startValue;
//            if (!int.TryParse(labelField.text, out startValue)) startValue = 0;
//            DOTween.Kill(labelField);
//            DOTween.To(() => startValue, value => labelField.text = value.ToString("N0"), endValue, speed);
//        }


        public static Sequence CreatePulseSequence(this RectTransform rectTransform, float scale, float time,
            float delay)
        {
            return DOTween.Sequence()
                .Append(rectTransform
                    .DOScale(Vector3.one * scale, time)
                    .SetEase(Ease.InOutBack))
                .Append(rectTransform
                    .DOScale(Vector3.one, time)
                    .SetEase(Ease.Linear))
                .AppendInterval(delay)
                .SetLoops(-1, LoopType.Restart)
                .SetAutoKill(false)
                .Pause();
        }

        public static Sequence CreateGlowSequence(this CanvasGroup Group, float scale, float time)
        {
            return DOTween.Sequence()
                .Append(((RectTransform) Group.transform)
                    .DOScale(Vector3.one * scale, time / 2)
                    .SetEase(Ease.OutQuad))
                .Join(Group
                    .DOFade(1 / scale, time / 2)
                    .SetEase(Ease.OutQuad))
                .Append(((RectTransform) Group.transform)
                    .DOScale(Vector3.one, time / 2)
                    .SetEase(Ease.InQuad))
                .Join(Group
                    .DOFade(1f, time / 2)
                    .SetEase(Ease.InQuad))
                .SetLoops(-1, LoopType.Restart)
                .SetAutoKill(false)
                .Pause();
        }

        public static Sequence CreateRotateSequence(this RectTransform rectTransform, float angle = 360,
            float speed = 1)
        {
            return DOTween.Sequence()
                .Append(rectTransform
                    .DORotate(Vector3.back * angle, speed, RotateMode.FastBeyond360)
                    .SetEase(Ease.Linear))
                .SetLoops(-1, LoopType.Restart);
        }

        public static Sequence CreateFlyTextSequence(this RectTransform rectTransform, TextMeshProUGUI textComponent,
            float scale, float showTime, float hideTime, Vector2 endPosition, Color endColor, float scaleOvershoot)
        {
            return DOTween.Sequence()
                .Append(rectTransform.DOScale(rectTransform.localScale * scale, showTime)
                    .SetEase(Ease.OutBack, scaleOvershoot))
                .Join(rectTransform.DOAnchorPos(endPosition, showTime).SetEase(Ease.InOutSine))
                .Append(DOTween.To(() => textComponent.color, a => textComponent.color = a, endColor, hideTime)
                    .SetEase(Ease.InQuad));
        }
    }
}