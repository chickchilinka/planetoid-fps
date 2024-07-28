 #if UNITY_ANDROID

using System;
using Cysharp.Threading.Tasks;
using Unity.Notifications.Android;
using Notice.Data;
using UnityEngine;

 namespace Notice.Handler
{
    public class AndroidNoticeHandler : INoticeHandler
    {
        private const string SmallNotificationIconId = "notif_icon_small";
        private const string LargeNotificationIconId = "notif_icon_large";
        
        private AndroidNotificationChannel _androidNotificationChannel;

        public void ClearNotifications()
        {
            AndroidNotificationCenter.CancelAllNotifications();
        }

        public void ClearAllDisplayedNotifications()
        {
            AndroidNotificationCenter.CancelAllDisplayedNotifications();
        }

        public string GetChannelThatLaunchGame()
        {
            return AndroidNotificationCenter.GetLastNotificationIntent()?.Channel;
        }

        public async void Initialize()
        {
            var request = new PermissionRequest();
            await UniTask.WaitWhile(() => request.Status == PermissionStatus.RequestPending);
            Debug.Log($"[Notice] Notification permission status on android {request.Status}");
        }

        public void Schedule(NoticeData noticeData)
        {
            RegisterChannel(noticeData.ChannelId);
            
            var notification = new AndroidNotification
            {
                Text = noticeData.BodyText,
                Title = noticeData.TitleText,
                FireTime = DateTime.Now.AddSeconds(noticeData.DelaySeconds),
                SmallIcon = SmallNotificationIconId,
                LargeIcon = LargeNotificationIconId,
            };

            AndroidNotificationCenter.SendNotification(notification, noticeData.ChannelId);
        }

        private void RegisterChannel(string channelId)
        {
            AndroidNotificationCenter.DeleteNotificationChannel(channelId);
            
            _androidNotificationChannel = new AndroidNotificationChannel(
                channelId,
                channelId,
                channelId,
                Importance.Default);

            AndroidNotificationCenter.RegisterNotificationChannel(_androidNotificationChannel);
        }
    }
}
#endif
