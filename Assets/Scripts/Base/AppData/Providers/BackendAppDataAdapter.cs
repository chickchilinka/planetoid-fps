using System;
using System.Collections.Generic;
using Base.AppData.Data;
using Base.AppData.Interfaces;
using Cysharp.Threading.Tasks;

namespace Base.AppData.Providers
{
    public class BackendAppDataProvider : IAppDataProvider
    {
        public UniTask<IReadOnlyDictionary<string, AppModuleData>> Load(string[] keys)
        {
            throw new NotImplementedException();
        }

        public UniTask<IReadOnlyDictionary<string, int>> GetVersions(string[] keys)
        {
            throw new NotImplementedException();
        }

        public UniTask<bool> IsAvailable()
        {
            throw new NotImplementedException();
        }
    }
}