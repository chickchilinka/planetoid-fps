#if UNITY_IOS

using System;
using Notice.Data;
using Unity.Notifications.iOS;

namespace Notice.Handler
{
    public class IOSNoticeHandler : INoticeHandler
    {
        public void ClearNotifications()
        {
            iOSNotificationCenter.RemoveAllDeliveredNotifications();
            iOSNotificationCenter.RemoveAllScheduledNotifications();
        }

        public string GetChannelThatLaunchGame()
        {
            return iOSNotificationCenter.GetLastRespondedNotification()?.ThreadIdentifier;
        }

        public void ClearAllDisplayedNotifications(){
            iOSNotificationCenter.RemoveAllDeliveredNotifications();
        }

        public void Initialize()
        {
        }

        public void Schedule(NoticeData noticeData)
        {
            var timeTrigger = new iOSNotificationTimeIntervalTrigger
            {
                TimeInterval = new TimeSpan(0, 0, noticeData.DelaySeconds),
                Repeats = false
            };
            
            var notification = new iOSNotification
            {
                ThreadIdentifier = noticeData.ChannelId,
                Title = noticeData.TitleText,
                Body = noticeData.BodyText,
                ShowInForeground = true,
                Trigger = timeTrigger,
            };

            iOSNotificationCenter.ScheduleNotification(notification);
        }
    }
}
#endif