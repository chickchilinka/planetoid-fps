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
using Scene.Data;
using Spine;
using Spine.Unity;
using TMPro;
using UniRx;

namespace Scene.View
{
    [AttributeViewType(ViewType.Window)]
    public class TransitionWindow : BaseViewWithInput<TransitionWindow.Data>
    {
#pragma warning disable 0649
        [SerializeField] private Image _simpleLoader;
        [SerializeField] private TextMeshProUGUI _loadingText;

        public class Data : IViewInput
        {
        }

        public override ViewLayer ViewLayer => ViewLayer.Global;

        private const float SimpleEndTransitionDuration = 1f;

        private IViewService _viewService;
        private ISceneLoader _sceneLoader;
        private AddressableAssetsService _service;

        private string _animationName;
        private Skin[] _skins;
        private TrackEntry _track;
        private IDisposable _animationSubscription;
        private IDisposable _loadingSubscription;
        private float _effectDuration;

        private bool _isLoadingCompleted;
        private DownloadProgressPopUp _downloadProgressPopUp;

        [Inject]
        public void Construct(IViewService viewService, ISceneLoader sceneLoader,
            AddressableAssetsService service)
        {
            _viewService = viewService;
            _sceneLoader = sceneLoader;
            _service = service;
        }

        public override void Show(Data data)
        {
            base.Show(data);

            _loadingSubscription = _sceneLoader.Status.Where(status => status == SceneLoaderStatus.Loaded)
                .Subscribe(_ => LoadingCompleted());

            ShowTransition();
        }

        private async void ShowTransition()
        {
            _isLoadingCompleted = false;

            _simpleLoader.color = Color.white;

            _loadingText.gameObject.SetActive(true);

            if (_service.IsDownloadingAnyAsset)
            {
                _viewService.ShowView<DownloadProgressPopUp, DownloadProgressPopUp.Data>(
                    new DownloadProgressPopUp.Data(_service.DownloadProgress));

                _downloadProgressPopUp = _viewService.GetActiveView<DownloadProgressPopUp>();
            }

            while (!_isLoadingCompleted)
            {
                await Task.Yield();
            }

            if (_downloadProgressPopUp != null)
            {
                _viewService.HideView(_downloadProgressPopUp);
                _downloadProgressPopUp = null;
            }

            _loadingText.gameObject.SetActive(false);

            _simpleLoader.DOColor(Color.clear, SimpleEndTransitionDuration).SetEase(Ease.InOutSine);

            await Task.Delay(TimeSpan.FromSeconds(SimpleEndTransitionDuration));


            _loadingSubscription?.Dispose();

            _viewService.HideView<TransitionWindow>();
        }

        private void LoadingCompleted()
        {
            _isLoadingCompleted = true;
        }
    }
}