using ApplicationMode.States;

namespace ApplicationMode.Types
{
    public class GameMode : AbstractMode 
    {
        protected override IGameState[] GetStates()
        {
            return new []
            {
                Factory.Resolve<LoadTestSceneState>()
            };
        }
    }
}