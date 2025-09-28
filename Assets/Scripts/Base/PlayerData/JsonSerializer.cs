using Base.PlayerData.Interfaces;
using Newtonsoft.Json;

namespace Base.PlayerData
{
    public class JsonSerializer : ISerializer
    {
        public string Serialize<T>(T data)
        {
            return JsonConvert.SerializeObject(data);
        }

        public bool TryDeserialize<T>(string serializedData, out T data)
        {
            try
            {
                data = JsonConvert.DeserializeObject<T>(serializedData);
                return true;
            }
            catch
            {
                data = default;
                return false;
            }
        }
    }
}