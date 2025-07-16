
using Base.Localization.Services;
using Base.Pool.Utils;
using Pool;
using Zenject;

namespace Localization
{
    public class LocalizationInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.InstallAsSingle<LocalizationService>();
        }
    }
}