using System.Collections.Generic;
using System.Linq;
using AddressableAssetsSystem.Services;
using Utils.Extensions;
using Zenject;

namespace AddressableAssetsSystem.Handler
{
    public abstract class AbstractAddressableAssetsHandler : IAddressableAssetsHandler
    {
        public abstract string Label { get; }

        public abstract bool IsActualPack { get; }
        public abstract bool IsMustBeLoaded { get; }

        protected SignalBus SignalBus;
        private AssetsModel _assetsModel;

        [Inject]
        private void Construct(SignalBus signalBus, AssetsModel assetsModel)
        {
            _assetsModel = assetsModel;
            SignalBus = signalBus;
        }

        public IEnumerable<string> GetActualAssets()
        {
            if (IsActualPack)
                return GetAssetsByLabel(Label);
            return Enumerable.Empty<string>();
        }

        private IEnumerable<string> GetAssetsByLabel(string label)
        {
            if (!_assetsModel.CatalogInitialized)
                yield break;
            var settings = _assetsModel.Catalog.GetSettingsesByLabel(label);
            if (!settings.IsNullOrEmpty())
            {
                foreach (var setting in settings)
                    yield return setting.Name;
            }
        }

        public virtual void Initialize()
        {
            
        }

        public virtual void Dispose()
        {
            
        }
    }
}