using System.Collections.Generic;
using Base.PlayerData.Interfaces;

namespace Base.PlayerData
{
    public class PlayerDataConsumersCollector : IPlayerDataConsumersCollector
    {
        private readonly IEnumerable<IPlayerDataConsumer> _consumers;

        public PlayerDataConsumersCollector(IEnumerable<IPlayerDataConsumer> consumers)
        {
            _consumers = consumers;
        }

        public IEnumerable<IPlayerDataConsumer> CollectImplementations() => _consumers;
    }
}