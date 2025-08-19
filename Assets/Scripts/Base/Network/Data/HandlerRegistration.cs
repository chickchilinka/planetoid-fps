using System;

namespace Base.Network.Data
{
    public class HandlerRegistration
    {
        public Action Register { get; }
        public Action Unregister { get; }

        public HandlerRegistration(Action register, Action unregister)
        {
            Register = register;
            Unregister = unregister;
        }
    }
}