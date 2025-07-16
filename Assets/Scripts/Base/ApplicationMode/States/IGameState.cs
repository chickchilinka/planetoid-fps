using System;
using Cysharp.Threading.Tasks;

namespace ApplicationMode.States
{
    public interface IGameState
    {
        event Action<bool> Applied;
        string LocalizationKey { get; }
        
        bool StopSequenceOnFail { get; }
        
        UniTaskVoid Apply();
        void Clear();
    }
}