using Base.Pool.Utils;
using Pool;
using Scene.Rules;
using Zenject;

namespace Scene
{
    public class SceneInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.Bind<ISceneLoader>().To<SceneLoader>().AsSingle();

            Container.InstallAsSingle<ShowLoaderRule>();
        }
    }
}