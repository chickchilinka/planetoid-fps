

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