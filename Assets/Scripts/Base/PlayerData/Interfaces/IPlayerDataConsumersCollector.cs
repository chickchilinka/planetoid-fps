using System.Collections.Generic;

namespace Base.PlayerData.Interfaces
{
    public interface IPlayerDataConsumersCollector
    {
        IEnumerable<IPlayerDataConsumer> CollectImplementations();
    }
}