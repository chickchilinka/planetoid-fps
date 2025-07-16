using Scene;
using Scene.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Utils;
using Utils.Debugger;

public class GameMenu
{
    private const string EditorPath = "Game/";
    private const string MainGamePath = "Game/";
    private const string MenuPath = "Game/";

    [MenuItem(MenuPath + "Clear/All")]
    public static void ClearAll()
    {
        Caching.ClearCache();
        PlayerPrefs.DeleteKey(Const.Keys.PrefsPlayerDataKey);
        PlayerPrefs.DeleteKey(Const.Keys.PrefsGameDataKey);
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
            
        EditorSceneManager.OpenScene(string.Format(SceneLoader.ScenePathFormat, "Preloader"), OpenSceneMode.Single);
        EditorApplication.isPlaying = true;
    }
}