using Pool;
using Zenject;

namespace EffectSystem
{
    public class EffectsInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.InstallAsSingle<EffectsController>();
            Container.InstallAsSingle<EffectFactory>();

            Container.DeclareSignal<EffectsSignals.AddHolder>();
            Container.DeclareSignal<EffectsSignals.RemoveHolder>().OptionalSubscriber();
        }
    }
}