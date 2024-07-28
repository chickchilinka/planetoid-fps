namespace Registry
{
    public abstract class AbstractPluginPlatformData : IRegistryData
    {
        public abstract string PlatformName { get; }

        public string AndroidSdkKey;
        public string AppleSdkKey;
        
        public string Id => PlatformName;
    }
}