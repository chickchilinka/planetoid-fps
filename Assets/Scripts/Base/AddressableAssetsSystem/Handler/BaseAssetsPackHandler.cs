using AddressableAssetsSystem.Data;

namespace AddressableAssetsSystem.Handler
{
    public class BaseAssetsPackHandler : AbstractAddressableAssetsHandler
    {
        public override string Label => AssetLabelConst.BaseAssetsPack;
        
        public override bool IsActualPack => true;

        public override bool IsMustBeLoaded => IsActualPack;
    }
}
