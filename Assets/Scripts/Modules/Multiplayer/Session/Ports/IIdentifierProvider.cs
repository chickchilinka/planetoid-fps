using System;

namespace Modules.Multiplayer.Session
{
    public interface IIdentifierProvider
    {
        Guid NewGuid();
    }
}
