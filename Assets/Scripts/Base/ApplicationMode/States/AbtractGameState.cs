using System;

namespace ApplicationMode.States
{
    public abstract class AbstractGameState : IGameState
    {
        public event Action<bool> Applied;

        public abstract string LocalizationKey { get; }
        public abstract bool StopSequenceOnFail { get; }
        public abstract void Apply();


        public virtual void Clear()
        {
            Applied = null;
        }
        
        protected void OnApplied(bool success)
        {
            Applied?.Invoke(success);
        }
    }
}