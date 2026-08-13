using System;
using Base.Network.Utils;
using Modules.Multiplayer.Session;
using UnityEngine;
using Zenject;

namespace Modules.Multiplayer.Session.Networking
{
    /// <summary>Role-specific composition point for Base.Network lobby messages.</summary>
    public sealed class SessionNetworkInstaller : AbstractNetworkMonoInstaller
    {
        protected override void InstallCommon(DiContainer container)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            container.RegisterGlobalMessageType<SetReadyMessage>(SessionMessageTypeIds.SetReady);
            container.RegisterGlobalMessageType<JoinAcceptedMessage>(SessionMessageTypeIds.JoinAccepted);
            container.RegisterGlobalMessageType<SessionSnapshotMessage>(SessionMessageTypeIds.Snapshot);
            container.RegisterGlobalMessageType<SessionCommandRejectedMessage>(SessionMessageTypeIds.CommandRejected);
        }

        protected override void InstallServer(DiContainer container)
        {
            container.Bind<SessionConnectionRegistry>().AsSingle();
            container.BindInterfacesAndSelfTo<ServerSessionNetworkBridge>().AsSingle();
            container.Bind<ISessionEventPublisher>().To<ServerSessionSnapshotPublisher>().AsSingle();
            container.BindInterfacesAndSelfTo<ServerSessionConnectionProvider>().AsSingle();
            container.Bind<ISessionTelemetry>().To<SessionStructuredLogger>().AsSingle();
            container.RegisterGlobalConnectionMessageHandler<SetReadyMessageHandler, SetReadyMessage>();
        }

        protected override void InstallClient(DiContainer container)
        {
            container.BindInterfacesAndSelfTo<ClientSessionNetworkBridge>().AsSingle();
            container.RegisterGlobalClientMessageHandler<SessionSnapshotMessageHandler, SessionSnapshotMessage>();
            container.RegisterGlobalClientMessageHandler<JoinAcceptedMessageHandler, JoinAcceptedMessage>();
            container.RegisterGlobalClientMessageHandler<SessionCommandRejectedMessageHandler, SessionCommandRejectedMessage>();
        }
    }
}
