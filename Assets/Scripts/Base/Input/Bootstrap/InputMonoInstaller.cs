using Base.Input.Services;
using Zenject;

namespace Base.Input.Bootstrap
{
    public class InputMonoInstaller: MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<InputService>().AsSingle();
        }
    }
}