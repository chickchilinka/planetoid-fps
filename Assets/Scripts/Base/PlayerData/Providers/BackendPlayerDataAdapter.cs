using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Base.PlayerData.Data;
using Base.PlayerData.Interfaces;

namespace Base.PlayerData.Providers
{
    public class BackendPlayerDataAdapter : IPlayerDataProvider
    {
        public UniTask<IReadOnlyDictionary<string, PlayerModuleData>> Load(string[] keys)
        {
            throw new System.NotImplementedException();
        }

        public UniTask Save(PlayerModuleData[] data)
        {
            throw new System.NotImplementedException();
        }

        public UniTask<IReadOnlyDictionary<string, int>> GetVersions(string[] keys)
        {
            throw new System.NotImplementedException();
        }

        public UniTask<bool> IsAvailable()
        {
            throw new System.NotImplementedException();
        }
    }
}