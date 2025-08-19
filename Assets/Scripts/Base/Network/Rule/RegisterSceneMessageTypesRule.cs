using System;
using System.Collections.Generic;
using Base.Network.Data;
using Base.Network.Storage;
using Base.Network.Utils;
using Zenject;

namespace Base.Network.Rule
{
    public sealed class RegisterSceneMessageTypesRule : IInitializable, IDisposable
    {
        private readonly IList<MessageTypeRegistration> _regs;
        private readonly MessageTypeRegistry _registry;

        public RegisterSceneMessageTypesRule([Inject(Id = NetScopes.Scene)] IList<MessageTypeRegistration> regs,
            MessageTypeRegistry registry)
        {
            _regs = regs ?? Array.Empty<MessageTypeRegistration>();
            _registry = registry;
        }

        public void Initialize() => _registry.AddRange(_regs);
        public void Dispose() => _registry.RemoveRange(_regs);
    }
}