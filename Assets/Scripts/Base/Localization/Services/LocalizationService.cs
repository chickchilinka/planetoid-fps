using System;
using System.Collections.Generic;
using System.Linq;
using Base.Localization.Data;
using Base.Localization.Registry;
using UnityEngine;
using Utils.Debugger;

namespace Base.Localization.Services
{
    public class LocalizationService
    {
        private readonly LocalizationSettingsRegistry _localizationRegistry;

        private readonly Dictionary<string, string> _localizationDictionary = new Dictionary<string, string>();

        public string LanguageCode { get; private set; }
        public LocalizationSettings LocalizationData { get; private set; }
        public event Action OnLanguageChanged;

        public LocalizationService(LocalizationSettingsRegistry localizationRegistry)
        {
            _localizationRegistry = localizationRegistry;
        }

        public string GetText(string id)
        {
            if (string.IsNullOrEmpty(id))
                return String.Empty;

            if (_localizationDictionary == null || !_localizationDictionary.ContainsKey(id))
            {
                if (!string.IsNullOrEmpty(id))
                    PrintLog.Warn(LogTag.Localization, $"There is no localization data with id: {id}");

                return id;
            }

            return _localizationDictionary[id];
        }

        public bool ContainsKey(string id)
        {
            return _localizationDictionary != null && _localizationDictionary.ContainsKey(id);
        }

        public IEnumerable<LocalizationSettings> AvailableLanguages()
        {
            return _localizationRegistry.GetItems();
        }

        public void SetLanguage(string languageCode)
        {
            LanguageCode = string.IsNullOrEmpty(languageCode)
                ? GetLanguageCode(Application.systemLanguage)
                : languageCode;

            UpdateLocalizationData();
            OnLanguageChanged?.Invoke();
        }

        private void UpdateLocalizationData()
        {
            LocalizationData = GetLocalizationData(LanguageCode);
            var registry = LocalizationData?.LocalizationSettingsRegistry;

            if (registry == null)
            {
                PrintLog.Warn(LogTag.Localization, $"There is no registry for language {LanguageCode}");
                return;
            }

            var items = registry.GetItems();
            _localizationDictionary.Clear();

            foreach (var item in items)
            {
                if (_localizationDictionary.ContainsKey(item.Key))
                    PrintLog.Error(LogTag.Localization,$"Element with key {item.Key} already exists in localization!");
                else
                    _localizationDictionary.Add(item.Key, item.Value);
            }
        }

        private string GetLanguageCode(SystemLanguage language)
        {
            return _localizationRegistry.GetItems().FirstOrDefault(item => item.SystemLanguage == language)?.Code;
        }

        private LocalizationSettings GetLocalizationData(string code)
        {
            return _localizationRegistry.GetItems().FirstOrDefault(item => item.Code == code)
                   ?? _localizationRegistry.GetItems()
                       .FirstOrDefault(item => item.Code == _localizationRegistry.BaseLocalizationCode);
        }
    }
}