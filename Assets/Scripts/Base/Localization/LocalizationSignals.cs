namespace Localization
{
    public class LocalizationSignals
    {
        public class ChangeLanguageRequest
        {
            public string LanguageId { get; }
            
            public ChangeLanguageRequest(string languageId)
            {
                LanguageId = languageId;
            }
        }

        public class LanguageChanged
        {
        }
    }
}
