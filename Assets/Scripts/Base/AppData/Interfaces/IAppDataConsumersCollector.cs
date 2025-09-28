using System.Collections.Generic;

namespace Base.AppData.Interfaces
{
    public interface IAppDataConsumersCollector
    {
        IEnumerable<IAppDataConsumer> CollectImplementations();
    }
}