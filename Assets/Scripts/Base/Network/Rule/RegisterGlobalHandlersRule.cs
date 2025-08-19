using System;
using System.Collections.Generic;
using Base.Network.Data;
using Base.Network.Utils;
using Zenject;

namespace Base.Network.Rule
{
    public sealed class RegisterGlobalHandlersRule : IInitializable
    {
        private readonly IList<HandlerRegistration> _regs;

        public RegisterGlobalHandlersRule([Inject(Id = NetScopes.Global)] IList<HandlerRegistration> regs)
            => _regs = regs ?? Array.Empty<HandlerRegistration>();

        public void Initialize()
        {
            foreach (var r in _regs) 
                r.Register();
        }
    }
}