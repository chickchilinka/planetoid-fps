using ApplicationMode.Types;
using UnityEngine;
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
            _appMode.Apply();
        }
    }
}