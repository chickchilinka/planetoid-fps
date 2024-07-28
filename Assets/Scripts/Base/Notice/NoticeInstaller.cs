using Notice.Handler;
using Zenject;

namespace Notice
{
    public class NoticeInstaller : Installer
    {
        public override void InstallBindings()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Bind<AndroidNoticeHandler>();
#elif UNITY_IOS && !UNITY_EDITOR
            Bind<IOSNoticeHandler>();
#elif UNITY_EDITOR            
            Bind<UnityNoticeHandler>();
#endif
        }

        private void Bind<THandler>() where THandler : INoticeHandler
        {
            Container
                .Bind<INoticeHandler>()
                .To<THandler>()
                .AsSingle();
        }
    }
}