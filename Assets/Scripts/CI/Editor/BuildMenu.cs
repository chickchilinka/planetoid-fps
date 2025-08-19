using Features.FishNetworking.Impl.Data;
using MarrowMachine.Tools;
using UnityEditor;

namespace CI.Editor
{
    public static class BuildMenu
    {
        [MenuItem("Custom/CI/Build Client", false, 1)]
        public static void BuildClient()
        {
            var fishnetSettings = FindFishnetSettings();
            fishnetSettings.IsServer = false;
            CIBuilder.PerformWindowsMonoBuild("windows/client/game.exe");
        }

        [MenuItem("Custom/CI/Build Server", false, 1)]
        public static void BuildServer()
        {
            var fishnetSettings = FindFishnetSettings();
            fishnetSettings.IsServer = true;
            CIBuilder.PerformWindowsMonoBuild("windows/server/game.exe");
        }

        private static FishNetGeneralSettings FindFishnetSettings()
        {
            var guids = AssetDatabase.FindAssets("t:FishNetGeneralSettings");
            if (guids.Length == 0)
            {
                throw new System.Exception("No fishnet settings found");
            }

            if (guids.Length > 1)
            {
                throw new System.Exception("More than one fishnet settings found");
            }
            
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<FishNetGeneralSettings>(path);
        }
    }
}