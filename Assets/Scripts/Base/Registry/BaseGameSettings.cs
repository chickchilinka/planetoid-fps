namespace Registry
{
    public abstract class BaseGameSettings<TSettings> : RegistryBase<TSettings>
        where TSettings : class
    {
        protected bool UpdateData(TSettings settings)
        {
            if (settings == null)
                return false;
            
            RegistryData = settings;
            
            return true;
        }
    }
}