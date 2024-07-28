using System;

namespace Registry
{
    [Serializable]
    public class PluginEnvironmentData<TPlatfomData> : IRegistryData
        where TPlatfomData : AbstractPluginPlatformData
    {
        public string Environment;
        public TPlatfomData[] Platforms;
        
        public string Id => Environment;
    }
}