using System;
using Registry;
using UnityEngine.SceneManagement;

namespace Scene.Data
{
    [Serializable]
    public class SceneSettings : IRegistryData
    {
        public LoadSceneMode SceneMode;
        public SceneType SceneType;

        public string Id => SceneType.ToString();
    }
}