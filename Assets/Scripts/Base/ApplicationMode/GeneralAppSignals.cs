namespace Base.ApplicationMode
{
    public class GeneralAppSignals
    {
        public class ChangeLoadingText
        {
            public string Key { get; }
            
            public ChangeLoadingText(string key)
            {
                Key = key;
            }
        }
        

        public class BugReportOpened
        {
        }
        
        public class PlayGameRequest
        {
        }

        public class RestartGameRequest
        {
            
        }

        public class AppFocusChanged
        {
            public bool IsInFocus { get; }

            public AppFocusChanged(bool isInFocus)
            {
                IsInFocus = isInFocus;
            }
        }

        public class ApplicationQuit
        {
        }

        public class SessionStarted
        {
        }

        public class LastActionRequest
        {
        }
    }
}