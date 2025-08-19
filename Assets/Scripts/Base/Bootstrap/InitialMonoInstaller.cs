using AddressableAssetsSystem.Bootstrap;
using ApplicationMode;
using Base.EffectSystem.Bootstrap;
using Base.Pool.Bootstrap;
using Base.Pool.Utils;
using General;
using Pool;
using Scene;
using ViewSystem;
using Zenject;

namespace Bootstrap
{
    public class InitialMonoInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            SignalBusInstaller.Install(Container);
            Container.Install<ViewInstaller>();
            Container.Install<PoolInstaller>();
            Container.Install<SceneInstaller>();
            Container.Install<AppModeInstaller>();
            Container.Install<EffectsInstaller>();
        }
    }
}