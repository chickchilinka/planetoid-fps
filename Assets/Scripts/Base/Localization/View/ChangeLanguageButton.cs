using Base.Localization.Services;
using UnityEngine;
using ViewSystem.Button;
using Zenject;

namespace Base.Localization.View
{
    public class ChangeLanguageButton : AbstractButton
    {
        [SerializeField] 
        private string _localizationCode;
        
        private LocalizationService _localizationService;
        [Inject]
        public void Construct(LocalizationService localizationService)
        {
            _localizationService = localizationService;
        }
        
        public override void Activate()
        {
            _localizationService.SetLanguage(_localizationCode);
        }
    }
}