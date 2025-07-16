

using Zenject;

namespace AddressableAssetsSystem.Utils
{
    public static class ZenjectUtils
    {
        public static void InstallAsSingle<TType>(this DiContainer container)
        {
            container
                .BindInterfacesAndSelfTo<TType>()
                .AsSingle();
        }
        
        public static void InstallAsSingle<TInterface, TType>(this DiContainer container) where TType : TInterface
        {
            container
                .Bind<TInterface>()
                .To<TType>()
                .AsSingle();
        }
    }
}