using System;
using Base.Common.Registry;
using Base.Localization.Data;
using UnityEngine;

namespace Base.Localization.Registry
{
    [Serializable]
    [CreateAssetMenu(fileName = "LocalizationRegistry", menuName = "Registry/Remote/Localization/Languages")]
    public class LocalizationSettingsRegistry : RegistryListBase<LocalizationSettings>
    {
        public string BaseLocalizationCode = "en";
    }
}