using Base.PlayerData.Interfaces;
using Base.PlayerData.Providers;
using Base.PlayerData.Services;
using Zenject;

namespace Base.PlayerData.Bootstrap
{
    public class PlayerDataInstaller: MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<PlayerDataService>().AsSingle();
            Container.Bind<IPlayerDataCache>().To<PlayerPrefsPlayerDataCache>().AsSingle();
            Container.Bind<IPlayerDataProvider>().To<BackendPlayerDataAdapter>().AsSingle();
            Container.BindInterfacesTo<JsonSerializer>().AsSingle();
            Container.BindInterfacesTo<PlayerDataConsumersCollector>().AsSingle();
        }
    }
}