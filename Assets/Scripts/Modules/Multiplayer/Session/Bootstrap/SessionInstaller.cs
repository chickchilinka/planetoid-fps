using System;
using Zenject;

namespace Modules.Multiplayer.Session
{
    public static class SessionInstaller
    {
        public static void InstallServer(DiContainer container, SessionConfiguration configuration)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            container.BindInstance(configuration).AsSingle();
            container.Bind<SessionCommandQueue>().AsSingle();
            container.Bind<IServerSessionFacade>().To<ServerSessionFacade>().AsSingle();
        }
    }
}
