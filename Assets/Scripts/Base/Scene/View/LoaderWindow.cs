using System;
using AddressableAssetsSystem.Services;
using Base.ApplicationMode;
using General;
using Localization.View;
using UniRx;
using UnityEngine;
using UnityEngine.Serialization;
using Utils;
using ViewSystem;
using ViewSystem.Attributes;
using ViewSystem.Base;
using Zenject;

namespace Scene.View
{
    [AttributeViewType(ViewType.Window)]
    public class LoaderWindow : BaseView
    {
        [SerializeField] private LocalizationText _loadingText;
        [SerializeField] private SpineProgressView _animatedProgressbar;
        [Space]
        [SerializeField] [Range(1, 10)] private int _maxDots;
        [SerializeField] [Range(0.01f, 3f)] private float _intervalSecondsBetweenDots;
        [SerializeField] private string _loadingFormatKey = "loadingFormatKey";
        [SerializeField] private string _assetsLoadingKey = "assetsLoadingKey";


        public override ViewLayer ViewLayer => ViewLayer.Global;

        private const float FullProgressDuration = 1f;
        private const float ProgressChangeDuration = 0.1f;
        private const int BytesInMb = 1024 * 1024;
        private SignalBus _signalBus;
        private AddressableAssetsService _service;

        private IDisposable _progressStream;
        private IDisposable _loadingSequence;
        private readonly CompositeDisposable _subscriptions = new();

        private int _dotsCount;
        private string _dotsText;

        private string _loadingTextKey;

        [Inject]
        public void Construct(SignalBus signalBus, AddressableAssetsService service)
        {
            _signalBus = signalBus;
            _service = service;
        }

        public override void Show()
        {
            base.Show();
            _signalBus.GetStream<GeneralAppSignals.ChangeLoadingText>()
                .Subscribe(OnChangeLoadingText)
                .AddTo(_subscriptions);
            _service.DownloadStarted
                .Subscribe(_ => Start())
                .AddTo(_subscriptions);
            
            _animatedProgressbar.Initialize();
            _animatedProgressbar.PlayFinishAnimation();

            ChangeProgress(1f, FullProgressDuration);

            _loadingTextKey = _loadingFormatKey;
            _dotsCount = 0;

            UpdateDotsView();
        }

        private void Start()
        {
            _animatedProgressbar.Initialize();
            _loadingSequence?.Dispose();

            if (_service.IsDownloadingAnyAsset)
                _progressStream = _service.DownloadProgress.Subscribe(x =>
                {
                    ChangeProgress(x, ProgressChangeDuration);
                    _loadingText.SetFormat(_assetsLoadingKey,
                        (_service.DownloadAssetsSize * x / BytesInMb).ToString("F2"),
                        (_service.DownloadAssetsSize / BytesInMb).ToString("F2"));
                });
        }

        private void OnChangeLoadingText(GeneralAppSignals.ChangeLoadingText signal)
        {
            if (string.IsNullOrEmpty(signal.Key))
                return;

            _loadingTextKey = signal.Key;
            UpdateText();

            _loadingSequence?.Dispose();
            _progressStream?.Dispose();

            ChangeProgress(1f, FullProgressDuration);
            _loadingSequence = Observable.Interval(TimeSpan.FromSeconds(_intervalSecondsBetweenDots))
                .Subscribe(_ => UpdateDotsView());
        }

        private void ChangeProgress(float progress, float duration)
        {
            _animatedProgressbar.ChangeProgress(progress, duration);
        }
        
        public override void Hide(Action onEndHiding)
        {
            _subscriptions.Clear();
            
            _progressStream?.Dispose();
            _loadingSequence?.Dispose();

            base.Hide(onEndHiding);
        }

        private void UpdateDotsView()
        {
            if (_dotsCount > _maxDots)
                _dotsCount = 0;

            _dotsText = string.Empty;

            for (var i = 0; i < _dotsCount; i++)
                _dotsText += '.';

            UpdateText();

            _dotsCount++;
        }

        private void UpdateText()
        {
            _loadingText.SetFormat(_loadingTextKey, _dotsText);
        }
    }
}