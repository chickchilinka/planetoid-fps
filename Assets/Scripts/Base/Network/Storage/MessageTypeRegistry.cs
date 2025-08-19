using System;
using System.Collections.Generic;
using System.Linq;
using Base.Network.Data;

namespace Base.Network.Storage
{
    public class MessageTypeRegistry
    {
        private readonly Dictionary<Type, (ushort id, Reliability rel)> _to = new();
        private readonly Dictionary<ushort, (Type type, Reliability rel)> _from = new();

        public void AddRange(IEnumerable<MessageTypeRegistration> registrations)
        {
            foreach (var r in registrations) 
                Add(r);
        }

        public void Add(MessageTypeRegistration registration)
        {
            if (_to.ContainsKey(registration.MessageType))
                throw new InvalidOperationException($"Message type {registration.MessageType} is already registered");
            if (_from.ContainsKey(registration.TypeId))
                throw new InvalidOperationException($"Message short type {registration.TypeId} is already registered");

            _to[registration.MessageType] = (registration.TypeId, registration.DefaultReliability);
            _from[registration.TypeId] = (registration.MessageType, registration.DefaultReliability);
        }

        public void RemoveRange(IEnumerable<MessageTypeRegistration> regs)
        {
            foreach (var r in regs)
                Remove(r);
        }

        public void Remove(MessageTypeRegistration r)
        {
            if (_to.TryGetValue(r.MessageType, out var e) && e.id == r.TypeId)
            {
                _to.Remove(r.MessageType);
                _from.Remove(r.TypeId);
            }
        }


        public ushort GetId<T>() where T : struct, IMessagePayload => _to[typeof(T)].id;
        public Type GetType(ushort id) => _from[id].type;
        public Reliability GetDefaultReliability(ushort id) => _from[id].rel;
        public Reliability GetDefaultReliability<T>() where T : struct, IMessagePayload => _to[typeof(T)].rel;
    }
}