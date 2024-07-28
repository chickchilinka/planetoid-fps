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
                Container.InstallAsSingle<LoadInitialSceneRule>();
                //Container.InstallAsSingle<HideLoaderRule>();
                
                Container.DeclareSignal<SceneSignals.LoadingStarted>().OptionalSubscriber();
                Container.DeclareSignal<SceneSignals.LoadingRequested>().OptionalSubscriber();
                Container.DeclareSignal<SceneSignals.LoadingCompleted>().OptionalSubscriber();
                Container.DeclareSignal<SceneSignals.AdditionalSceneLoadingStarted>().OptionalSubscriber();
                Container.DeclareSignal<SceneSignals.AdditionalSceneLoadingCompleted>().OptionalSubscriber();
                Container.DeclareSignal<SceneSignals.UnloadingStarted>().OptionalSubscriber();
                Container.DeclareSignal<SceneSignals.LoadInitialScene>().OptionalSubscriber();
        }
    }
}