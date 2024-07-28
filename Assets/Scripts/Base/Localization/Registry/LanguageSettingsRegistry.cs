using System;
using Localization.Data;
using Registry;
using UnityEngine;

namespace Localization.Registry
{
    [Serializable]
    [CreateAssetMenu(fileName = "LocalizationSettingsRegistry", menuName = "Registry/Remote/Localization/LanguageSettings")]
    public class LanguageSettingsRegistry : RegistryListBase<LanguageSettings>
    {
        public void UpdateRegistry(LanguageSettings[] localizationSettingses)
        {
            SetItems(localizationSettingses);
        }
    }
}
