using UnityEngine;

namespace Base.Common.Rule
{
    public abstract class AbstractRule : IRule
    {
        public abstract void Initialize();
        public abstract void Dispose();

        protected void LogError(string msg)
        {
            Debug.LogWarning($"[{GetType().Name}] {msg}");
        }
    }
}