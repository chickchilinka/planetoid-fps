using System;
using Registry;

namespace Base.Localization.Data
{
    [Serializable]
    public class TranslationData : IRegistryData
    {
        public string Id => Key;

        public string Key;
        public string Value;
    }
}