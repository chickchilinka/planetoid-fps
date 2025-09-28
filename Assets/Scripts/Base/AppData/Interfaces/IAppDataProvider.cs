using System.Collections.Generic;
using Base.AppData.Data;
using Cysharp.Threading.Tasks;

namespace Base.AppData.Interfaces
{
    public interface IAppDataProvider
    {
        UniTask<IReadOnlyDictionary<string, AppModuleData>> Load(string[] keys);
        UniTask<IReadOnlyDictionary<string, int>> GetVersions(string[] keys);
        UniTask<bool> IsAvailable();
    }
}