using System;
using UniRx;
using UnityEngine;
using ViewSystem.Animation;
using ViewSystem.Attributes;
using ViewSystem.Controller;
using Zenject;

namespace ViewSystem.Base
{
    [AttributeViewType(ViewType.Hint)]
    public class BaseHintView : BaseAnimatedView<FadeViewAnimation>
    {
#pragma warning disable 0649
        [Header("Animation")]
        [SerializeField] private CanvasGroup _canvasGroup;
        
        public override ViewLayer ViewLayer => ViewLayer.Hint;

        protected virtual bool IsHideOnTap { get; set; } = true;

        private IViewController _viewController;
        private IDisposable _tapStream;

        [Inject]
        public void Constrtuct(IViewController viewController)
        {
            _viewController = viewController;
        }
        
        protected override void InitializeAnimation()
        {
            Animation = new FadeViewAnimation(_canvasGroup);
        }

        public override void Show()
        {
            _tapStream?.Dispose();
            
            _tapStream = Observable.EveryUpdate()
                .Where(_ => Input.GetMouseButtonUp(0) && IsHideOnTap)
                .First()
                .Subscribe(_ =>
                {
                    _tapStream?.Dispose();
                    _viewController.HideView(GetType());
                });
            
            base.Show();
        }
        
        public override void Hide(Action onEndHiding)
        {
            _tapStream?.Dispose();
            base.Hide(onEndHiding);
        }
    }
}
