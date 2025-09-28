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
            
            Container.InstallAsSingle<LoadMenuSceneState>();
            Container.InstallAsSingle<ShowInitialWindowState>();
            Container.InstallAsSingle<LoadTestSceneState>();
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