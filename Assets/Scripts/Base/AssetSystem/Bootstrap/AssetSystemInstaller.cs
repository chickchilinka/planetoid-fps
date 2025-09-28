using Base.AssetSystem.Providers;
using Base.AssetSystem.Services;
using Base.AssetSystem.Storage;
using Zenject;

namespace Base.AssetSystem.Bootstrap
{
    public class AssetSystemMonoInstaller: MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<AssetService>().AsSingle();
            Container.Bind<AssetCache>().AsSingle()
                .WhenInjectedInto<AssetService>();
            Container.BindInterfacesTo<ResourcesAssetProvider>().AsSingle();
        }
    }
}