using System;

namespace ApplicationMode.Types
{
    public interface IAppMode
    {
        event Action<string> OnStateChanged; 
        void Apply();
    }
}