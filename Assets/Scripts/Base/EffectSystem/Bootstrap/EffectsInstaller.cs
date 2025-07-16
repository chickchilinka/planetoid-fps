using Base.EffectSystem.Factory;
using Base.EffectSystem.Services;
using Base.Pool.Utils;
using EffectSystem.Storage;
using Zenject;

namespace Base.EffectSystem.Bootstrap
{
    public class EffectsInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.InstallAsSingle<EffectService>();
            Container.InstallAsSingle<EffectFactory>();
            Container.InstallAsSingle<EffectsHolderStorage>();
        }
    }
}