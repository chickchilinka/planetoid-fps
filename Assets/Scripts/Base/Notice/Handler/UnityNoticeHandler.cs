using Notice.Data;
using UnityEngine;

namespace Notice.Handler
{
    public class UnityNoticeHandler : INoticeHandler
    {
        public void Initialize()
        {
            Log("initialized");
        }

        public void Schedule(NoticeData noticeData)
        {
            Log("scheduled " + noticeData.ChannelId);
        }

        public void ClearNotifications()
        {
            Log("cleared");
        }

        public void ClearAllDisplayedNotifications()
        {
            Log("cleared all displayed notifications");
        }

        public string GetChannelThatLaunchGame()
        {
            return null;
        }

        private void Log(string message)
        {
            Debug.Log($"[Notice] {message}");
        }
    }
}