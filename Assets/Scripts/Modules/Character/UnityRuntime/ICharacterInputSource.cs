using Modules.Character.Simulation;

namespace Modules.Character.UnityRuntime
{
    public interface ICharacterInputSource
    {
        CharacterInput ConsumeForTick();
    }
}
