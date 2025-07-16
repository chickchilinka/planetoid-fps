using System;
using UniRx;
using UnityEngine;
using ViewSystem.Controller;
using Zenject;

namespace ViewSystem.Base
{
    public class AnimatedFadeTransitionWithInput:AnimatedFadeView<AnimatedFadeTransitionWithInput.FadeTransitionData>
    {
        private FadeTransitionData _data;
        [SerializeField] private float _delay;
        private IDisposable _disposable;
        private IViewService _viewService;
        [Inject]
        public void Construct(IViewService viewService)
        {
            _viewService = viewService;
        }
        
        public class FadeTransitionData: IViewInput
        {
            public Action Transit { get; }

            public FadeTransitionData(Action onTransit)
            {
                Transit = onTransit;
            }
        }
        public override void Show(FadeTransitionData data)
        {
            _data = data;
            base.Show(data);
        }
        protected override void OnEndAnimatingShow()
        {
            _disposable = Observable.Timer(TimeSpan.FromSeconds(_delay)).Subscribe(_=>
            {
                Transit();
                _disposable.Dispose();
                _viewService.HideView(this);
            });
        }
        private void Transit()
        {
            _data.Transit();
        }
    }
}