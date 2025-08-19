using System;
using System.Collections.Generic;
using Base.Network.Data;
using Base.Network.Storage;
using Base.Network.Utils;
using Zenject;

namespace Base.Network.Rule
{
    public sealed class RegisterGlobalMessageTypesRule : IInitializable
    {
        private readonly IList<MessageTypeRegistration> _regs;
        private readonly MessageTypeRegistry _registry;

        public RegisterGlobalMessageTypesRule([Inject(Id = NetScopes.Global)] IList<MessageTypeRegistration> regs,
            MessageTypeRegistry registry)
        {
            _regs = regs ?? Array.Empty<MessageTypeRegistration>();
            _registry = registry;
        }

        public void Initialize() => _registry.AddRange(_regs);
    }
}