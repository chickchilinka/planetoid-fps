using System;
using Localization.Registry;
using Registry;
using UnityEngine;

namespace Localization.Data
{
    [Serializable]
    public class LocalizationSettings : IRegistryData
    {
        public string Id => Code;
        public string Code;
        public string Title;
        public Sprite Icon;
        public LanguageSettingsRegistry LocalizationSettingsRegistry;
    }
}