using System.Collections.Generic;
using System.Linq;
using AddressableAssetsSystem.Services;
using Base.ApplicationMode;
using Cysharp.Threading.Tasks;
using General;
using Zenject;

namespace ApplicationMode.States
{
    public abstract class AbstractAddressableState : AbstractGameState
    {
        private SignalBus _signalBus;
        
        protected AddressableAssetsService AddressableAssetsService;

        public override bool StopSequenceOnFail => false;
        public override string LocalizationKey => "initAssets";

        [Inject]
        protected void Construct(SignalBus signalBus, AddressableAssetsService addressableAssetsService)
        {
            AddressableAssetsService = addressableAssetsService;

            _signalBus = signalBus;
        }

        public override async UniTaskVoid Apply()
        {
            await AddressableAssetsService.Initialize();
            await AddressableAssetsService.UpdateCatalog();
            await AddressableAssetsService.CacheLocalAssets();
            
            DownloadActualAssets();
        }
        
        private void DownloadActualAssets()
        {
            var assetNames = GetAssetNames();
            AddressableAssetsService.TryDownloadRemoteAssets(assetNames.ToArray(), DownloadCompleted);
        }
        
        private async void DownloadCompleted(bool isSuccess)
        {
            if (!isSuccess)
            {
                AddressableAssetsService.ContinueWithError(DownloadActualAssets);
                return;
            }
            
            await AddressableAssetsService.CacheAssetsAsync(GetAssetNames().ToArray());
            
            _signalBus.Fire(new GeneralAppSignals.ChangeLoadingText("connectingAssets"));

            OnApplied(true);
        }

        protected abstract IEnumerable<string> GetAssetNames();
    }
}