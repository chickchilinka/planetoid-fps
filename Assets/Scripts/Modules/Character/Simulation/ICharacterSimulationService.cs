namespace Modules.Character.Simulation
{
    public interface ICharacterSimulationService
    {
        CharacterStepResult Simulate(
            in CharacterInput input,
            in CharacterBodySnapshot body,
            in CharacterSimulationState state,
            float tickDelta);
    }
}
