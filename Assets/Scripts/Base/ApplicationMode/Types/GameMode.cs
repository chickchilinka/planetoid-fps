using ApplicationMode.States;

namespace ApplicationMode.Types
{
    public class GameMode : AbstractMode 
    {
        protected override IGameState[] GetStates()
        {
            return new []
            {
                Factory.Resolve<StartSessionState>(),

                Factory.Resolve<DelayLoadingState>(),

                // Factory.Resolve<DownloadActualAddressablesState>(),

                Factory.Resolve<DelayLoadingState>(),

                Factory.Resolve<ShowInitialWindowState>()
            };
        }
    }
}