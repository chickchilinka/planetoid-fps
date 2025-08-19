using System.ComponentModel;
using Base.Network.Service;
using UnityEngine;
using Zenject;

namespace Base.Network.Utils
{
    public abstract class AbstractNetworkMonoInstaller: MonoBehaviour
    {
        public void Install(DiContainer container, bool isServer)
        {
            InstallCommon(container);
            
            if (isServer)
            {
                InstallServer(container);
            }
            else
            {
                InstallClient(container);
            }
        }

        protected abstract void InstallCommon(DiContainer container);
        protected abstract void InstallClient(DiContainer container);
        protected abstract void InstallServer(DiContainer container);
    }
}