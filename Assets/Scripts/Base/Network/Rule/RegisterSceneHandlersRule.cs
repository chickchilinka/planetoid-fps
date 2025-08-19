using System;
using System.Collections.Generic;
using Base.Network.Data;
using Base.Network.Routing;
using Base.Network.Utils;
using Zenject;

namespace Base.Network.Rule
{
    public class RegisterSceneHandlersRule : IInitializable, IDisposable
    {
        private readonly IList<HandlerRegistration> _regs;

        public RegisterSceneHandlersRule([Inject(Id = NetScopes.Scene)] IList<HandlerRegistration> regs)
            => _regs = regs ?? Array.Empty<HandlerRegistration>();

        public void Initialize()
        {
            foreach (var r in _regs) 
                r.Register();
        }

        public void Dispose()
        {
            foreach (var r in _regs) 
                r.Unregister();
        }
    }
}