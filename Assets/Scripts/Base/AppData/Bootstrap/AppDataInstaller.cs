using Base.AppData.Interfaces;
using Base.AppData.Providers;
using Base.AppData.Services;
using Modules.AppData;
using Zenject;

namespace Base.AppData.Bootstrap
{
    public class AppDataInstaller: MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<AppDataService>().AsSingle();
            Container.Bind<IAppDataCache>().To<PlayerPrefsAppDataCache>().AsSingle();
            Container.Bind<IAppDataProvider>().To<BackendAppDataProvider>().AsSingle();
            Container.BindInterfacesTo<AppDataConsumersCollector>().AsSingle();
            Container.BindInterfacesTo<JsonSerializer>().AsSingle();
        }
    }
}