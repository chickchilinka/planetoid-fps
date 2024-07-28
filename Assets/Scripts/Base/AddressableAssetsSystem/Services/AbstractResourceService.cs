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
