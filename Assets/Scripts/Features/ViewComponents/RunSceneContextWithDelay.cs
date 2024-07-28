using System.Collections;
using UnityEngine;
using Zenject;

namespace Utils.View
{
    [RequireComponent(typeof(SceneContext))]
    public class RunSceneContextWithDelay : MonoBehaviour
    {
#pragma warning disable 0649
        [SerializeField] private float _delay;

        private SceneContext _sceneContext;

        private void Awake()
        {
            _sceneContext = GetComponent<SceneContext>();

            StartCoroutine(InstallAllLater());
        }

        protected virtual IEnumerator InstallAllLater()
        {
            yield return new WaitForSeconds(_delay);
            _sceneContext.Run();
        }
    }
}