using ApplicationMode.States;
using Zenject;

namespace ApplicationMode
{
    public class AppModeFactory
    {
        private readonly DiContainer _diContainer;

        public AppModeFactory(DiContainer diContainer)
        {
            _diContainer = diContainer;
        }

        public IGameState Resolve<T>() where T : IGameState
        {
            return _diContainer.Resolve<T>();
        }
    }
}