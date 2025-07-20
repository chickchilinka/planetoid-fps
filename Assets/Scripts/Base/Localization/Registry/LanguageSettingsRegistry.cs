using System;
using Base.Common.Registry;
using Base.Localization.Data;
using UnityEngine;

namespace Base.Localization.Registry
{
    [Serializable]
    [CreateAssetMenu(fileName = "LocalizationSettingsRegistry", menuName = "Registry/Remote/Localization/LanguageSettings")]
    public class LanguageSettingsRegistry : RegistryListBase<TranslationData>
    {
        public void UpdateRegistry(TranslationData[] localizationSettingses)
        {
            SetItems(localizationSettingses);
        }
    }
}
