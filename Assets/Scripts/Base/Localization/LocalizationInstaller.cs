using Localization.Rule;
using Pool;
using Zenject;

namespace Localization
{
    public class LocalizationInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.InstallAsSingle<LocalizationService>();
            
            Container.InstallAsSingle<ChangeLanguageRule>();

            Container.DeclareSignal<LocalizationSignals.ChangeLanguageRequest>();
            Container.DeclareSignal<LocalizationSignals.LanguageChanged>().OptionalSubscriber();
        }
    }
}