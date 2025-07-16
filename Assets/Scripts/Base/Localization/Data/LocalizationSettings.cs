using System;
using Base.Localization.Registry;
using Registry;
using UnityEngine;

namespace Base.Localization.Data
{
    [Serializable]
    public class LocalizationSettings : IRegistryData
    {
        public string Id => Code;
        public string Code;
        public SystemLanguage SystemLanguage;
        public LanguageSettingsRegistry LocalizationSettingsRegistry;
    }
}