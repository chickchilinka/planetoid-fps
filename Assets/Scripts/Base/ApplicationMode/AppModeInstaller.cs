using ApplicationMode.States;
using ApplicationMode.Types;
using Base.ApplicationMode;
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

            Container.DeclareSignal<GeneralAppSignals.SessionStarted>();
            Container.DeclareSignal<GeneralAppSignals.AppFocusChanged>();
            Container.DeclareSignal<GeneralAppSignals.ApplicationQuit>();
            Container.DeclareSignal<GeneralAppSignals.ChangeLoadingText>();
            Container.DeclareSignal<GeneralAppSignals.PlayGameRequest>();
            Container.DeclareSignal<GeneralAppSignals.RestartGameRequest>();
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