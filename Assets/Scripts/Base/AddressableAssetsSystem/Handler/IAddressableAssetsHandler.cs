using System;
using System.Collections.Generic;
using Zenject;

namespace AddressableAssetsSystem.Handler
{
    public interface IAddressableAssetsHandler : IInitializable, IDisposable
    {
        string Label { get; }
        IEnumerable<string> GetActualAssets();
        bool IsActualPack { get; }
        bool IsMustBeLoaded { get; }
    }
}
