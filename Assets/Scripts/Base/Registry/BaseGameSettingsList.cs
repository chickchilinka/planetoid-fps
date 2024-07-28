using System.Linq;
using Registry;
using UnityEngine;

namespace General.Data
{
    public abstract class BaseGameSettingsList<TSettings> : RegistryListBase<TSettings>
        where TSettings : class, IRegistryData
    {
        protected bool UpdateData(TSettings[] settings)
        {
            if (settings == null)
                return false;

            var data = settings
                .Where(t => !string.IsNullOrEmpty(t.Id))
                .ToArray();

            if (data.Any())
                SetItems(data);
            else
                Debug.LogWarning($"All data are empty for {typeof(TSettings).Name}");

            return true;
        }
    }
}