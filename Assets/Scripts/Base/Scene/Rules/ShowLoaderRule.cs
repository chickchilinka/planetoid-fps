using System;
using Base.Common.Rule;
using Scene.Data;
using Scene.View;
using UniRx;
using ViewSystem.Controller;

namespace Scene.Rules
{
    public class ShowLoaderRule : AbstractRule
    {
        private readonly IViewService _viewService;
        private readonly ISceneLoader _sceneLoader;

        private IDisposable _subscription;

        public ShowLoaderRule(IViewService viewService, ISceneLoader sceneLoader)
        {
            _viewService = viewService;
            _sceneLoader = sceneLoader;
        }

        public override void Initialize()
        {
            _subscription = _sceneLoader.Status.Where(status => status == SceneLoaderStatus.Loading)
                .Subscribe(_ => ShowTransition());
        }

        public override void Dispose()
        {
            _subscription?.Dispose();
        }

        private void ShowTransition()
        {
            _viewService.ShowView<TransitionWindow, TransitionWindow.Data>(new TransitionWindow.Data()
                { });
        }
    }
}