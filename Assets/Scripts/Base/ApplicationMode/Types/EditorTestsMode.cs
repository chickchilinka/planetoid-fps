using ApplicationMode.States;

namespace ApplicationMode.Types
{
    public class EditorTestsMode : AbstractMode
    {
        protected override IGameState[] GetStates()
        {
            return new IGameState[0];
        }
    }
}