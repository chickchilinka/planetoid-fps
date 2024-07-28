using System;
using Registry;

namespace Localization.Data
{
    [Serializable]
    public class LanguageSettings : IRegistryData
    {
        public string Id => Key;

        public string Key;
        public string Value;
    }
}