using Notice.Data;

namespace Notice.Handler
{
    public interface INoticeHandler
    {
        void Initialize();
        void Schedule(NoticeData noticeData);
        void ClearNotifications();
        void ClearAllDisplayedNotifications();
        string GetChannelThatLaunchGame();
    }
}