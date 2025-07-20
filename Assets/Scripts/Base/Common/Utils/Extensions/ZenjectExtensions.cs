using Zenject;

namespace Base.Pool.Utils
{
    public static class ZenjectExtensions
    {
        public static void InstallFromInstanceAsSingle<TType>(this DiContainer container, TType registry)
        {
            container
                .BindInterfacesAndSelfTo<TType>()
                .FromInstance(registry)
                .AsSingle();
        }

        public static void InstallAsSingle<TType>(this DiContainer container)
        {
            container
                .BindInterfacesAndSelfTo<TType>()
                .AsSingle()
                .NonLazy();
        }

        public static void InstallAsSingle<TInterface, TType>(this DiContainer container) where TType : TInterface
        {
            container
                .Bind<TInterface>()
                .To<TType>()
                .AsSingle();
        }

        public static void InstallAsTransient<TInterface, TComponent>(this DiContainer container)
            where TComponent : TInterface
        {
            container
                .Bind<TInterface>()
                .To<TComponent>()
                .AsTransient();
        }

        public static void InstallAsTransient<TType>(this DiContainer container)
        {
            container
                .BindInterfacesAndSelfTo<TType>()
                .AsTransient();
        }

        public static void InstallAsTransient<TType>(this DiContainer container, object id)
        {
            container
                .Bind<TType>()
                .WithId(id)
                .AsTransient();
        }
        
        public static void InstallAsTransient<TInterface, TComponent>(this DiContainer container, object id) 
            where TComponent : TInterface
        {
            container
                .Bind<TInterface>()
                .WithId(id)
                .To<TComponent>()
                .AsTransient();
        }
        
        public static void InstallAsTransientWithComponentId<TInterface, TComponent>(this DiContainer container) 
            where TComponent : TInterface
        {
            container
                .Bind<TInterface>()
                .WithId(typeof(TComponent).FullName)
                .To<TComponent>()
                .AsTransient();
        }
    }
}