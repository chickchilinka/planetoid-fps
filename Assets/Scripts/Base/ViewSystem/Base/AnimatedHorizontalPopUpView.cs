
using UnityEngine;
using ViewSystem.Animation;
using ViewSystem.Attributes;
using Zenject;

namespace ViewSystem.Base
{
    [AttributeViewType(ViewType.Window)]
    public class AnimatedHorizontalPopUpView: BaseAnimatedView<HorizontalJumpAnimation>
    {
#pragma warning disable 0649

        [Header("Animation")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _panelRectTransform;
        
        public override ViewLayer ViewLayer => ViewLayer.PopUp;

        private SignalBus _signalBus;

        [Inject]
        private void Construct(SignalBus signalBus)
        {
            _signalBus = signalBus;
        }

        protected override void InitializeAnimation()
        {
            Animation = new HorizontalJumpAnimation(_canvasGroup, _panelRectTransform);
        }
    }
}