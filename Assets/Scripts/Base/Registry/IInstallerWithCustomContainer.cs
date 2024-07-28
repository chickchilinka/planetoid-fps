using Zenject;

namespace Registry
{
    public interface IInstallerWithCustomContainer
    {
        void InstallBindingsWithCustomContainer(DiContainer container);
    }
}
