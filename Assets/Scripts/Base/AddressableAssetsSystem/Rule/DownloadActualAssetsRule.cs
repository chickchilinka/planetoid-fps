// MarrowMachine CONFIDENTIAL
// __________________
//
// [2016] - [2024] MarrowMachine LLC
// All Rights Reserved.
//
// NOTICE:  All information contained herein is, and remains
// the property of MarrowMachine LLC and its suppliers,
// if any.  The intellectual and technical concepts contained
// herein are proprietary to MarrowMachine LLC
// and its suppliers and may be covered by U.S. and Foreign Patents,
// patents in process, and are protected by trade secret or copyright law.
// Dissemination of this information or reproduction of this material
// is strictly forbidden unless prior written permission is obtained
// from MarrowMachine LLC.

using AddressableAssetsSystem.Services;
using General;
using Rule;
using Scene;
using Scene.Data;
using UniRx;
using Zenject;

namespace AddressableAssetsSystem.Rule
{
    public class DownloadActualAssetsRule : AbstractRule
    {
        private readonly AddressableAssetsService _addressableAssetsService;
        private readonly SignalBus _signalBus;
        private readonly CompositeDisposable _disposable = new();

        private DownloadActualAssetsRule(SignalBus signalBus, AddressableAssetsService addressableAssetsService)
        {
            _signalBus = signalBus;
            _addressableAssetsService = addressableAssetsService;
        }
        
        public override void Initialize()
        {
            _signalBus.GetStream<GeneralGameSignals.PlayGameRequest>()
                .DelayFrame(1)
                .Subscribe(_=> PlayGameRequest())
                .AddTo(_disposable);
            _signalBus.GetStream<SceneSignals.LoadingCompleted>()
                .DelayFrame(1)
                .Subscribe(signal => SceneLoaded(signal.SceneType))
                .AddTo(_disposable);
        }

        public override void Dispose()
        {
            _disposable.Clear();
        }

        private void PlayGameRequest()
        {
            _addressableAssetsService.TryDownloadActualAssets();
        }

        private void SceneLoaded(SceneType sceneType)
        {
            if (sceneType is SceneType.Map) 
                _addressableAssetsService.TryDownloadActualAssets();
        }
    }
}