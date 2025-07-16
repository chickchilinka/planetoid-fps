using ApplicationMode.States;
using ApplicationMode.Types;
using Base.ApplicationMode;
using Base.Pool.Utils;
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
            Container.InstallAsSingle<LoadMapSceneState>();
            Container.InstallAsSingle<ShowInitialWindowState>();
            Container.InstallAsSingle<LoadTestSceneState>();
            Container.InstallAsSingle<StartSessionState>();
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