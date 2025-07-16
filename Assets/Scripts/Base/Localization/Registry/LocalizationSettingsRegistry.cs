using System;
using Base.Localization.Data;
using General.Data;
using UnityEngine;

namespace Base.Localization.Registry
{
    [Serializable]
    [CreateAssetMenu(fileName = "LocalizationRegistry", menuName = "Registry/Remote/Localization/Languages")]
    public class LocalizationSettingsRegistry : BaseGameSettingsList<LocalizationSettings>
    {
        public string BaseLocalizationCode = "en";
    }
}