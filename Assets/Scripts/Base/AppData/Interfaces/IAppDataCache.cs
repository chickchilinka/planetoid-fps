using Base.AppData.Data;
using Cysharp.Threading.Tasks;

namespace Base.AppData.Interfaces
{
    public interface IAppDataCache: IAppDataProvider
    {
        UniTask Save(AppModuleData data);
    }
}