// MarrowMachine CONFIDENTIAL
// __________________
// 
// [2016] - [2024] MarrowMachine LLC
// All Rights Reserved.
// 
// NOTICE:  All information contained herein is, and remains
// the property of MarrowMachine LLC and its suppliers,
// if any.  The intellectual and technical concepts contained
// here in are proprietary to MarrowMachine LLC and its suppliers and may be covered by U.S. and Foreign Patents,
// patents in process, and are protected by trade secret or copyright law.
// Dissemination of this information or reproduction of this material
// is strictly forbidden unless prior written permission is obtained
// from MarrowMachine LLC.

using UnityEngine.Networking;

namespace AddressableAssetsSystem.Utils
{
    public class AddressableCertificateHandler:CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true;
        }
    }
}