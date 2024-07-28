using System.Collections.Generic;

namespace ApplicationMode.States
{
    public class DownloadActualAddressablesState : AbstractAddressableState
    {
        protected override IEnumerable<string> GetAssetNames()
        {
            return AddressableAssetsService.GetActualAssets();
        }
    }
}