using System;

namespace ApplicationMode.States
{
    public interface IGameState
    {
        event Action<bool> Applied;
        string LocalizationKey { get; }
        
        bool StopSequenceOnFail { get; }
        
        void Apply();
        void Clear();
    }
}