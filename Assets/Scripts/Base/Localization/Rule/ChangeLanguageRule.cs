using Rule;

namespace Localization.Rule
{
    public class ChangeLanguageRule : AbstractSignalRule<LocalizationSignals.ChangeLanguageRequest>
    {
        private readonly LocalizationService _localizationService;

        public ChangeLanguageRule(LocalizationService localizationService)
        {
            _localizationService = localizationService;
        }
        
        protected override void OnSignalFired(LocalizationSignals.ChangeLanguageRequest signal)
        {
            _localizationService.SetLanguage(signal.LanguageId);
        }
    }
}