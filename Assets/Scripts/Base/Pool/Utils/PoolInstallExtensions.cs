using Base.Pool.Factory;
using UnityEngine;
using Zenject;
using IPoolable = Pool.IPoolable;

namespace Base.Pool.Utils
{
    public static class PoolInstallExtensions
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
        
        public static void InstallPoolWithComponentId<TComponent>(this DiContainer container, string resourcesPath, int size = 0) 
            where TComponent : MonoBehaviour, IPoolable
        {
            container.BindMemoryPool<TComponent, Pool<TComponent>>()
                .WithId(typeof(TComponent))
                .WithInitialSize(size)
                .FromComponentInNewPrefabResource(resourcesPath + typeof(TComponent).Name);
        }
        
        public static void InstallPoolWithInterfaceId<TInterface, TComponent>(this DiContainer container, string resourcesPath, int size = 0) 
            where TInterface : IPoolable
            where TComponent : MonoBehaviour, TInterface, IPoolable
        {
            container.BindMemoryPool<TInterface, Pool<TInterface>>()
                .WithId(typeof(TInterface))
                .WithInitialSize(size)
                .FromComponentInNewPrefabResource(resourcesPath + typeof(TComponent).Name);
        }
        
        public static void InstallPoolWithCustomId<TInterface, TComponent>(this DiContainer container, string resourcesPath, object id, int size = 0) 
            where TInterface : IPoolable
            where TComponent : MonoBehaviour, TInterface, IPoolable
        {
            container.BindMemoryPool<TInterface, Pool<TInterface>>()
                .WithId(id)
                .WithInitialSize(size)
                .FromComponentInNewPrefabResource(resourcesPath + typeof(TComponent).Name);
        }

        public static void InstallPoolWithCustomId<TComponent>(this DiContainer container, string resourcesPath, object id, int size = 0)
            where TComponent : MonoBehaviour, IPoolable
        {
            container.BindMemoryPool<TComponent, Pool<TComponent>>()
                .WithId(id)
                .WithInitialSize(size)
                .FromComponentInNewPrefabResource(resourcesPath + id);
        }

        public static void InstallPoolWithCustomIdByName<TInterface>(this DiContainer container, string resourcesPath, object id, int size = 0)
            where TInterface : IPoolable
        {
            container.BindMemoryPool<TInterface, Pool<TInterface>>()
                .WithId(id)
                .WithInitialSize(size)
                .FromComponentInNewPrefabResource(resourcesPath + id);
        }

        public static void InstallPoolWithComponentId<TInterface, TComponent>(this DiContainer container, string resourcesPath, int size = 0) 
            where TInterface : IPoolable
            where TComponent : MonoBehaviour, TInterface, IPoolable
        {
            container.BindMemoryPool<TInterface, Pool<TInterface>>()
                .WithId(typeof(TComponent).Name)
                .WithInitialSize(size)
                .FromComponentInNewPrefabResource(resourcesPath + typeof(TComponent).Name);
        }
    }
}