using System;
using Stability.Models;

namespace Stability
{
    public class StabilitySignals
    {
        public class HandleError
        {
            public ErrorData ErrorData { get; }
            public Action OnContinueOffline { get; }

            public HandleError(ErrorData errorData,Action onContinueOffline)
            {
                ErrorData = errorData;
                OnContinueOffline = onContinueOffline;
            }
            public HandleError(ErrorData errorData)
            {
                ErrorData = errorData;
            }
        }

        public class InitializeStability
        {
            
        }
    }
}