using System;
using UnityEngine;
using ViewSystem;
using ViewSystem.Attributes;
using ViewSystem.Base;
using Zenject;
using ViewSystem.Controller;
using System.Threading.Tasks;
using Addressables.View;
using AddressableAssetsSystem.Services;
using UnityEngine.UI;
using DG.Tweening;
using Spine;
using Spine.Unity;
using TMPro;

namespace Scene.View
{
    [AttributeViewType(ViewType.Window)]
    public class TransitionWindow : BaseViewWithInput<TransitionWindow.Data>
    {
#pragma warning disable 0649
        [SerializeField] private Image _simpleLoader;
        [SerializeField] private SkeletonGraphic _skeletonTransition;
        [SerializeField] private TextMeshProUGUI _loadingText;

        public class Data : IViewInput
        {
            public bool IsAnimatedTransition;
        }

        public override ViewLayer ViewLayer => ViewLayer.Global;

        private const float SimpleEndTransitionDuration = 1f;

        private IViewController _viewController;
        private SignalBus _signalBus;
        private AddressableAssetsService _service;

        private string _animationName;
        private Skin[] _skins;
        private TrackEntry _track;
        private IDisposable _animationStream;
        private float _effectDuration;

        private bool _isLoadingCompleted;
        private DownloadProgressPopUp _downloadProgressPopUp;

        [Inject]
        public void Construct(IViewController viewController, SignalBus signalBus, 
            AddressableAssetsService service)
        {
            _viewController = viewController;
            _signalBus = signalBus;
            _service = service;
        }

        public override void Show(Data data)
        {
            base.Show(data);

            _signalBus.TryUnsubscribe<SceneSignals.LoadingCompleted>(LoadingCompleted);
            _signalBus.Subscribe<SceneSignals.LoadingCompleted>(LoadingCompleted);

            ShowTransition(data.IsAnimatedTransition);
        }

        private async void ShowTransition(bool isAnimatedTransition)
        {
            _isLoadingCompleted = false;

            _simpleLoader.gameObject.SetActive(!isAnimatedTransition);
            _skeletonTransition.gameObject.SetActive(isAnimatedTransition);

            if (isAnimatedTransition)
            {
                _loadingText.gameObject.SetActive(false);

                _skins = _skeletonTransition.SkeletonData.Skins.Items;
                _animationName = _skeletonTransition.SkeletonData.Animations.Items[0].Name;
                _effectDuration = _skeletonTransition.SkeletonData.Animations.Items[0].Duration;

                var index = UnityEngine.Random.Range(1, _skins.Length);
                _skeletonTransition.Skeleton.SetSkin(_skins[index]);

                _track = _skeletonTransition.AnimationState.SetAnimation(0, _animationName, false);

                _track.TimeScale = 0;
            }
            else
                _simpleLoader.color = Color.white;

            _loadingText.gameObject.SetActive(true);

            if (_service.IsDownloadingAnyAsset)
            {
                _viewController.ShowView<DownloadProgressPopUp, DownloadProgressPopUp.Data>(
                    new DownloadProgressPopUp.Data(_service.DownloadProgress));

                _downloadProgressPopUp = _viewController.GetActiveView<DownloadProgressPopUp>();
            }
            
            while (!_isLoadingCompleted)
            {
                await Task.Yield();
            }

            if (_downloadProgressPopUp != null)
            {
                _viewController.HideView(_downloadProgressPopUp);
                _downloadProgressPopUp = null;
            }

            _loadingText.gameObject.SetActive(false);

            if (isAnimatedTransition)
            {
                _track.TimeScale = 1f;

                await Task.Delay(TimeSpan.FromSeconds(_effectDuration));
            }
            else
            {
                _simpleLoader.DOColor(Color.clear, SimpleEndTransitionDuration).SetEase(Ease.InOutSine);

                await Task.Delay(TimeSpan.FromSeconds(SimpleEndTransitionDuration));
            }

            _signalBus.Unsubscribe<SceneSignals.LoadingCompleted>(LoadingCompleted);

            _viewController.HideView<TransitionWindow>();
        }

        private void LoadingCompleted()
        {
            _isLoadingCompleted = true;
        }
    }
}