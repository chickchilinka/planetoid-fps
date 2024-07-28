using System;
using System.Linq;
using General.Data;
using Localization.Data;
using UnityEngine;

namespace Localization.Registry
{
    [Serializable]
    [CreateAssetMenu(fileName = "LocalizationRegistry", menuName = "Registry/Remote/Localization/Languages")]
    public class LocalizationSettingsRegistry : BaseGameSettingsList<LocalizationSettings>
    {
        public LocalizationSettings Default;

        public bool UpdateData(LocalizationData localizationData)
        {
            UpdateLocalization(localizationData.EnLocalizationSettings, "en");
            UpdateLocalization(localizationData.RuLocalizationSettings, "ru");

            return true;
        }

        private void UpdateLocalization(LanguageSettings[] localizationSettings, string code)
        {
            if (localizationSettings == null)
                return;
            
            var data = localizationSettings.Where(t => !string.IsNullOrEmpty(t.Id) && 
                                                       !string.IsNullOrEmpty(t.Value)).ToArray();

            if (data.Any())
            {
                var registryItems = GetItems().FirstOrDefault(item => item.Id.Equals(code));
                registryItems?.LocalizationSettingsRegistry.UpdateRegistry(localizationSettings);
            }
            else 
                Debug.LogWarning("All data empty for Localization with code " + code);
        }
    }
}