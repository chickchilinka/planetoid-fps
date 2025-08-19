using UnityEngine;

namespace Features.FishNetworking.Impl.Data
{
    [CreateAssetMenu(fileName = "FishNetGeneralSettings", menuName = "Config/FishNetGeneralSettings")]
    public class FishNetGeneralSettings: ScriptableObject
    {
        [field: SerializeField]
        public bool IsServer { get; set; }
    }
}