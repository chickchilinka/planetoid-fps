using ApplicationMode.States;
using ApplicationMode.Types;
using Pool;
using Zenject;

namespace ApplicationMode
{
    public class AppModeInstaller : Installer
    {
        public override void InstallBindings()
        {
            InstallAppMode();
            
            Container.InstallAsSingle<DownloadActualAddressablesState>();
            Container.InstallAsSingle<DownloadAllAddressablesState>();
            Container.InstallAsSingle<LoadMapScene>();
            Container.InstallAsSingle<ShowInitialWindowState>();
            Container.InstallAsSingle<StartSessionState>();
            Container.InstallAsSingle<DelayLoadingState>();
            Container.InstallAsSingle<AppModeFactory>();
        }
        
        private void InstallAppMode()
        {
            BindAppMode<GameMode>();
        }
        
        private void BindAppMode<TMode>() where TMode : IAppMode
        {
            Container.InstallAsSingle<IAppMode, TMode>();
        }
    }
}