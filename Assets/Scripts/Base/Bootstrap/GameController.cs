using ApplicationMode.Types;
using UnityEngine;
using Utils.Services;
using Zenject;

namespace General
{
    public class GameController : MonoBehaviour
    {
        private IAppMode _appMode;

        [Inject]
        public void Construct(IAppMode appMode)
        {
            _appMode = appMode;
        }

        private void Awake()
        {
            Application.targetFrameRate = Screen.currentResolution.refreshRate;
            _appMode.Apply();
        }
    }
}