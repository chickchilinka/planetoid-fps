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
