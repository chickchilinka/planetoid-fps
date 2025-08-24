using Base.Network.Data;
using Base.Network.Handler;
using Base.Network.Routing;
using Zenject;

namespace Base.Network.Utils
{
    public static class ZenjectNetworkExtensions
    {
        public static void RegisterClientMessageHandler<THandler, TMessage>(this DiContainer container)
            where THandler : IClientMessageHandler<TMessage> where TMessage : struct, IMessagePayload
        {
            container.BindInterfacesAndSelfTo<THandler>().AsSingle();
            container.Bind<HandlerRegistration>().WithId(NetScopes.Scene)
                .FromInstance(new HandlerRegistration(
                    register: () =>
                    {
                        var handler = container.Resolve<THandler>();
                        container.Resolve<ClientMessageRouter>().Register(handler);
                    },
                    unregister: () =>
                    {
                        var handler = container.Resolve<THandler>();
                        container.Resolve<ClientMessageRouter>().Unregister(handler);
                    }
                )).AsCached();
        }

        public static void RegisterConnectionMessageHandler<THandler, TMessage>(this DiContainer container)
            where THandler : IConnectionMessageHandler<TMessage> where TMessage : struct, IMessagePayload
        {
            container.BindInterfacesAndSelfTo<THandler>().AsSingle();
            container.Bind<HandlerRegistration>().WithId(NetScopes.Scene)
                .FromInstance(new HandlerRegistration(
                    register: () =>
                    {
                        var handler = container.Resolve<THandler>();
                        container.Resolve<ConnectionMessageRouter>().Register(handler);
                    },
                    unregister: () =>
                    {
                        var handler = container.Resolve<THandler>();
                        container.Resolve<ConnectionMessageRouter>().Unregister(handler);
                    }
                )).AsCached();
        }

        public static void RegisterMessageType<TMessage>(this DiContainer container, ushort id, Reliability reliability = Reliability.Reliable)
            where TMessage : struct, IMessagePayload
        {
            container.Bind<MessageTypeRegistration>().WithId(NetScopes.Scene)
                .FromInstance(new MessageTypeRegistration
                    { MessageType = typeof(TMessage), TypeId = id, DefaultReliability = reliability })
                .AsCached();
        }

        public static void RegisterGlobalClientMessageHandler<THandler, TMessage>(this DiContainer container)
            where THandler : IClientMessageHandler<TMessage> where TMessage : struct, IMessagePayload
        {
            container.BindInterfacesAndSelfTo<THandler>().AsSingle();
            container.Bind<HandlerRegistration>().WithId(NetScopes.Global)
                .FromInstance(new HandlerRegistration(
                    register: () =>
                    {
                        var handler = container.Resolve<THandler>();
                        container.Resolve<ClientMessageRouter>().Register(handler);
                    },
                    unregister: () =>
                    {
                        
                    }
                )).AsCached();
        }

        public static void RegisterGlobalConnectionMessageHandler<THandler, TMessage>(this DiContainer container)
            where THandler : IConnectionMessageHandler<TMessage> where TMessage : struct, IMessagePayload
        {
            container.BindInterfacesAndSelfTo<THandler>().AsSingle();
            container.Bind<HandlerRegistration>().WithId(NetScopes.Global)
                .FromInstance(new HandlerRegistration(
                    register: () =>
                    {
                        var handler = container.Resolve<THandler>();
                        container.Resolve<ConnectionMessageRouter>().Register(handler);
                    },
                    unregister: () => { }
                )).AsCached();
        }

        public static void RegisterGlobalMessageType<TMessage>(this DiContainer container, ushort id,
            Reliability reliability = Reliability.Reliable)
            where TMessage : struct, IMessagePayload
        {
            container.Bind<MessageTypeRegistration>().WithId(NetScopes.Global)
                .FromInstance(new MessageTypeRegistration
                    { MessageType = typeof(TMessage), TypeId = id, DefaultReliability = reliability })
                .AsCached();
        }
        
        public static void RegisterConnectionRequestHandler<THandler,TReq,TRes>(this DiContainer container)
            where THandler : IConnectionRequestHandler<TReq,TRes>
            where TReq : struct, IMessagePayload
            where TRes : struct, IMessagePayload
        {
            container.BindInterfacesAndSelfTo<THandler>().AsSingle();
            container.Bind<HandlerRegistration>().WithId(NetScopes.Scene)
                .FromInstance(new HandlerRegistration(
                    register: () => {
                        var handler = container.Resolve<THandler>();
                        container.Resolve<ConnectionRequestRouter>().Register(handler);
                    },
                    unregister: () => {
                        var handler = container.Resolve<THandler>();
                        container.Resolve<ConnectionRequestRouter>().Unregister(handler);
                    }
                )).AsCached();
        }
        
        public static void RegisterClientRequestHandler<THandler,TReq,TRes>(this DiContainer container)
            where THandler : IClientRequestHandler<TReq,TRes>
            where TReq : struct, IMessagePayload
            where TRes : struct, IMessagePayload
        {
            container.BindInterfacesAndSelfTo<THandler>().AsSingle();
            container.Bind<HandlerRegistration>().WithId(NetScopes.Scene)
                .FromInstance(new HandlerRegistration(
                    register: () => {
                        var handler = container.Resolve<THandler>();
                        container.Resolve<ClientRequestRouter>().Register(handler);
                    },
                    unregister: () => {
                        var handler = container.Resolve<THandler>();
                        container.Resolve<ClientRequestRouter>().Unregister(handler);
                    }
                )).AsCached();
        }
    }
}