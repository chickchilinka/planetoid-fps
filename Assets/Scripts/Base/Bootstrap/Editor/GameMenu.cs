using Scene;
using Scene.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Utils;
using Utils.Debugger;

public class GameMenu
{
    // private static readonly Vector2Int GameResolution = new Vector2Int(1440, 2880);
    // private static readonly Vector2Int EditorResolution = new Vector2Int(2224, 1880);

    private const string EditorPath = "Game/";
    private const string MainGamePath = "Game/";
    private const string MenuPath = "Game/";
    [MenuItem(EditorPath + "Build/OSX")]
    public static void BuildEditorOsX()
    {
        BuildEditor(BuildTarget.StandaloneOSX, "app");
    }
    
    [MenuItem(EditorPath + "Build/Windows")]
    public static void BuildEditorWindows()
    {
        BuildEditor(BuildTarget.StandaloneWindows, "exe");
    }

    [MenuItem(MenuPath + "Clear/All")]
    public static void ClearAll()
    {
        Caching.ClearCache();
        PlayerPrefs.DeleteKey(Const.Keys.PrefsPlayerDataKey);
        PlayerPrefs.DeleteKey(Const.Keys.PrefsGameDataKey);
        // PlayerPrefs.DeleteKey(Const.Keys.PrefsGameDataInfoKey);
        PlayerPrefs.DeleteKey(Const.Keys.FieldActionsRecorderJson);
        PlayerPrefs.Save();
    }
    
    [MenuItem(MenuPath + "Clear/Player data %#&C")]
    public static void ClearPlayerData()
    {
        Caching.ClearCache();
        PlayerPrefs.DeleteKey(Const.Keys.PrefsPlayerDataKey);
        PlayerPrefs.Save();
        PrintLog.Info($"{LogTag.System} PlayerData wiped from the device!");
    }
    
    [MenuItem(MenuPath + "Clear/Game data")]
    public static void ClearGameData()
    {
        Caching.ClearCache();
        PlayerPrefs.DeleteKey(Const.Keys.PrefsGameDataKey);
        // PlayerPrefs.DeleteKey(Const.Keys.PrefsGameDataInfoKey);
        PlayerPrefs.Save();
    }
        
    [MenuItem(MainGamePath + "🎮 Run Game")]
    public static void RunGame()
    {
        RunTheGame();
    }
    
    private static void RunTheGame()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.SaveOpenScenes();
            
        EditorSceneManager.OpenScene(string.Format(SceneLoader.ScenePathFormat, SceneType.Preloader), OpenSceneMode.Single);
        EditorApplication.isPlaying = true;
    }
    
    private static void BuildEditor(BuildTarget buildTarget, string extension)
    {
        var path = EditorUtility.SaveFolderPanel("Choose Location of Built Game", "", "");
        
        if (string.IsNullOrEmpty(path))
            return;
        
        var levels = new []
        {
            $"Assets/Scenes/{SceneType.Preloader}.unity", 
            $"Assets/Scenes/{SceneType.Initial}.unity",
            $"Assets/Scenes/{SceneType.Game}.unity",
            $"Assets/Scenes/{SceneType.LevelEditor}.unity",
        };
        BuildPipeline.BuildPlayer(levels, path + $"/DL_editor.{extension}", buildTarget, BuildOptions.None);
    }
}