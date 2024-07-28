using UnityEngine;
using ViewSystem.Animation;
using ViewSystem.Attributes;

namespace ViewSystem.Base
{
    [AttributeViewType(ViewType.Window)]
    public class AnimatedFadeView<TViewInput>:BaseAnimatedViewWithInput<TViewInput, FadeViewAnimation> where TViewInput : IViewInput
    {
        [Header("Animation")]
        [SerializeField] private CanvasGroup _canvasGroup;
        public override ViewLayer ViewLayer { get; }

        protected override void InitializeAnimation()
        {
            Animation = new FadeViewAnimation(_canvasGroup);
        }
    }
}