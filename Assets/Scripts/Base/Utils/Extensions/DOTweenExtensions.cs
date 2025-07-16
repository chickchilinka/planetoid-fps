using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Utils.Extensions
{
    public static class DOTweenExtensions
    {
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

        public static Sequence CreateGlowSequence(this CanvasGroup group, float scale, float time)
        {
            return DOTween.Sequence()
                .Append(((RectTransform) group.transform)
                    .DOScale(Vector3.one * scale, time / 2)
                    .SetEase(Ease.OutQuad))
                .Join(group
                    .DOFade(1 / scale, time / 2)
                    .SetEase(Ease.OutQuad))
                .Append(((RectTransform) group.transform)
                    .DOScale(Vector3.one, time / 2)
                    .SetEase(Ease.InQuad))
                .Join(group
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
    }
}