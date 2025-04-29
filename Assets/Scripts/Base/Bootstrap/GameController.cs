using ApplicationMode.Types;
using UnityEngine;
using Utils.Debugger;
using Utils.Services;
using Zenject;

namespace General
{
    public class GameController : IInitializable
    {
        private readonly IAppMode _appMode;

        public GameController(
            IAppMode appMode)
        {
            _appMode = appMode;
        }

        public void Initialize()
        {
            Application.targetFrameRate = Screen.currentResolution.refreshRate;
            
            RandomService.Initialize();
            
            PrintLog.Info(LogTag.GameInfo,
                // $"Version: {GameSet.Version}\n" +
                                           $"Mode: {_appMode.GetType().Name}\n" +
                                           // $"Environment: {_gameSettings.Environment}\n" +
                                           // $"Platform: {GameSet.DeviceType}\n" +
                                           $"Screen: {Screen.currentResolution}\n" 
                                           // + $"Cheats: {(_cheatService.AreCheatsOn ? "on" : "off")}"
                );
            
            _appMode.Apply();
        }
    }
}