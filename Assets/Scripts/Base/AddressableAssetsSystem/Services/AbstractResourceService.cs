namespace AddressableAssetsSystem.Services
{
    public abstract class AbstractResourceService<TType> where TType : UnityEngine.Object
    {
        private readonly AddressableAssetsService _addressableAssetsService;

        protected AbstractResourceService(AddressableAssetsService addressableAssetsService)
        {
            _addressableAssetsService = addressableAssetsService;
        }

        public TType GetCachedAsset(string asset)
        {
            return _addressableAssetsService.GetCachedAsset<TType>(asset);
        }

        public bool CanGetCachedAsset(string asset)
        {
            return _addressableAssetsService.CanGetCachedAsset(asset);
        }
    }
}
