using System.Collections.Generic;
using Base.AppData.Interfaces;

namespace Modules.AppData
{
    public class AppDataConsumersCollector : IAppDataConsumersCollector
    {
        private readonly IEnumerable<IAppDataConsumer> _consumers;

        public AppDataConsumersCollector(IEnumerable<IAppDataConsumer> consumers)
        {
            _consumers = consumers;
        }

        public IEnumerable<IAppDataConsumer> CollectImplementations()
        {
            return _consumers;
        }
    }
}