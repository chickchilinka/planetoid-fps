using Zenject;

namespace Pool
{
    public class PoolInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.Bind<GeneralPool>().AsSingle();
        }
    }
}
