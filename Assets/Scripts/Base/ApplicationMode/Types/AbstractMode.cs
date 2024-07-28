using ApplicationMode.States;
using General;
using Utils.Debugger;
using Zenject;

namespace ApplicationMode.Types
{
    public abstract class AbstractMode : IAppMode
    {
        protected AppModeFactory Factory { get; private set; }

        private IGameState[] _initializeSequence;
        private const int DefaultIndex = -1;

        private int _index = DefaultIndex;

        private SignalBus _signalBus;
        
        [Inject]
        protected void Construct(AppModeFactory factory, SignalBus signalBus)
        {
            _signalBus = signalBus;
            Factory = factory;
        }

        public void Apply()
        {
            _initializeSequence = GetStates();
            ApplyNextIndex(true);
        }

        private void ApplyNextIndex(bool success)
        {
            if (_index >= 0)
            {
                var lastGameState = _initializeSequence[_index];
                LogInfo($"[finish] {lastGameState.GetType().Name} with {success}");
                lastGameState.Clear();

                if (lastGameState.StopSequenceOnFail && !success)
                {
                    InitializationCompleted();
                    return;
                }
            }
            
            _index++;

            if (_index >= _initializeSequence.Length)
            {
                InitializationCompleted();
                return;
            }
            
            var nextGameState = _initializeSequence[_index];
            nextGameState.Applied += ApplyNextIndex;
            LogInfo($"[start] {nextGameState.GetType().Name}");
            nextGameState.Apply();
            _signalBus.Fire(new GeneralGameSignals.ChangeLoadingText(nextGameState.LocalizationKey));
        }
        
        protected abstract IGameState[] GetStates();

        private void InitializationCompleted()
        {
            _index = DefaultIndex;
        }
        
        private void LogInfo(string message)
        {
            PrintLog.Info(LogTag.Init, message);
        }
    }
}