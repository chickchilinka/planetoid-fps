using UnityEngine;
using ViewSystem.Animation;
using ViewSystem.Attributes;
using Zenject;

namespace ViewSystem.Base
{
    [AttributeViewType(ViewType.Window)]
    public class AnimatedPopUpWithInputView<TViewInput> : BaseAnimatedViewWithInput<TViewInput, VerticalJumpAnimation>
        where TViewInput : IViewInput
    {
#pragma warning disable 0649
        [Header("Animation")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _panelRectTransform;

        private SignalBus _signalBus;

        [Inject]
        private void Construct(SignalBus signalBus)
        {
            _signalBus = signalBus;
        }

        public override ViewLayer ViewLayer => ViewLayer.PopUp;

        protected override void InitializeAnimation()
        {
            Animation = new VerticalJumpAnimation(_canvasGroup, _panelRectTransform);
        }
    }
}
