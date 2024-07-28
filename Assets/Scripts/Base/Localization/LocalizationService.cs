using System.Collections.Generic;
using System;
using System.Linq;
using Localization.Data;
using Localization.Registry;
using Player;
using UnityEngine;
using Zenject;

namespace Localization
{
    public class LocalizationService : ISerializablePlayerData
    {
        private readonly LocalizationSettingsRegistry _localizationRegistry;
        private readonly SignalBus _signalBus;

        private readonly Dictionary<string, string> _localizationDictionary = new Dictionary<string, string>();

        public string LanguageCode { get; private set; }
        public LocalizationSettings LocalizationData { get; private set; }

        public LocalizationService(SignalBus signalBus, 
            LocalizationSettingsRegistry localizationRegistry)
        {
            _localizationRegistry = localizationRegistry;
            _signalBus = signalBus;

            AddCallendars();
            SetLanguage(string.Empty);
        }

        public void AddCallendars()
        {
            new System.Globalization.GregorianCalendar();
            new System.Globalization.PersianCalendar();
            new System.Globalization.UmAlQuraCalendar();
            new System.Globalization.ThaiBuddhistCalendar();
        }

        public string GetText(string id)
        {
            if (string.IsNullOrEmpty(id))
                return String.Empty;

            if (_localizationDictionary == null || !_localizationDictionary.ContainsKey(id))
            {
                if (!string.IsNullOrEmpty(id))
                    Debug.LogWarning($"There is no localization data with id: {id}");

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
            LanguageCode = string.IsNullOrEmpty(languageCode) ? GetLanguageCode(Application.systemLanguage) : languageCode;

            UpdateLocalizationData();
            _signalBus.TryFire(new LocalizationSignals.LanguageChanged());
        }

        private void UpdateLocalizationData()
        {
            LocalizationData = GetLocalizationData(LanguageCode);
            var registry = LocalizationData?.LocalizationSettingsRegistry;

            if (registry == null)
            {
                Debug.LogWarning($"[LocalizationService] There is no registry for language {LanguageCode}");
                return;
            }

            var items = registry.GetItems();
            _localizationDictionary.Clear();

            foreach (var item in items)
            {
                if (_localizationDictionary.ContainsKey(item.Key))
                    Debug.LogError($"Element with key {item.Key} already exists in localization!");
                else
                    _localizationDictionary.Add(item.Key, item.Value);
            }
        }

        private string GetLanguageCode(SystemLanguage language)
        {
            /*
            if (language == SystemLanguage.Russian)
                return "ru";
            
            if (language == SystemLanguage.Portuguese)
                return "pt";
            if (language == SystemLanguage.French)
                return "fr";
            if (language == SystemLanguage.Spanish)
                return "es";
            if (language == SystemLanguage.German)
                return "de";
            */

            return _localizationRegistry.Default.Id;
        }

        private LocalizationSettings GetLocalizationData(string languageId)
        {
            return _localizationRegistry.GetItems().FirstOrDefault(item => item.Code == languageId)
                    ?? _localizationRegistry.GetItems().FirstOrDefault(item => item.Code == _localizationRegistry.Default.Code);
        }

        public void LoadPlayerData(PlayerData playerData)
        {
            throw new NotImplementedException();
        }

        public void GetPlayerData(PlayerData playerData)
        {
            throw new NotImplementedException();
        }
    }
}
