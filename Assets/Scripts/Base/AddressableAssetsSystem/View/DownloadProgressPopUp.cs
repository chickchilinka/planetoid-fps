using System;
using Features.ViewComponents;
using TMPro;
using UniRx;
using UnityEngine;
using ViewSystem;
using ViewSystem.Attributes;
using ViewSystem.Base;

namespace Addressables.View
{

    [AttributeViewType(ViewType.Hint)]
    public class DownloadProgressPopUp : AnimatedPopUpWithInputView<DownloadProgressPopUp.Data>
    {
#pragma warning disable 0649
        [Space]
        [SerializeField] private AnimatedSliderView _animatedSlider;
        [SerializeField] private TextMeshProUGUI _progress;
        
        private IDisposable _progressStream;

        public override ViewLayer ViewLayer => ViewLayer.Global;

        public class Data : IViewInput
        {
            public IReadOnlyReactiveProperty<float> Progress { get; }

            public Data(IReadOnlyReactiveProperty<float> progress)
            {
                Progress = progress;
            }
        }

        public override void Show(Data data)
        {
            base.Show(data);

            ChangeProgress(data.Progress.Value, true);

            _progressStream = data.Progress.Subscribe(x => ChangeProgress(x));
        }

        private void ChangeProgress(float progress, bool force = false)
        {
            _animatedSlider.ChangeValue(progress, force);
            _progress.text = (int)(progress * 100) + "%";
        }

        public override void Hide(Action onEndHiding)
        {
            _progressStream?.Dispose();
            base.Hide(onEndHiding);
        }
    }
}