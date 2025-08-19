using Base.Common.Log;
using Scene;
using Scene.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Utils;

public class GameMenu
{
    private const string MainGamePath = "Game/";
    private const string MenuPath = "Game/";

    [MenuItem(MenuPath + "Clear/All")]
    public static void ClearAll()
    {
        Caching.ClearCache();
        PlayerPrefs.Save();
    }
    
    [MenuItem(MenuPath + "Clear/Player data %#&C")]
    public static void ClearPlayerData()
    {
        Caching.ClearCache();
        PlayerPrefs.Save();
        PrintLog.Info($"{LogTag.System} PlayerData wiped from the device!");
    }
    
    [MenuItem(MenuPath + "Clear/Game data")]
    public static void ClearGameData()
    {
        Caching.ClearCache();
        PlayerPrefs.Save();
    }
        
    [MenuItem(MainGamePath + "🎮 Run Local Game")]
    public static void RunGame()
    {
        RunTheGame();
    }

    [MenuItem(MainGamePath + "Run Server")]
    public static void RunServer()
    {
        
    }

    [MenuItem(MainGamePath + "🎮Run Client")]
    public static void RunClient()
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