using Modules.SurfaceGravity.Core;
using UnityEngine;

namespace Modules.SurfaceGravity.UnityRuntime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class GravitySurfaceView : MonoBehaviour
    {
        [SerializeField] private string _surfaceId;
        [SerializeField] private Collider _gravityCollider;

        public SurfaceId SurfaceId => new SurfaceId(_surfaceId);

        public Collider GravityCollider => _gravityCollider;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_gravityCollider == null)
                _gravityCollider = GetComponent<Collider>();

            if (string.IsNullOrWhiteSpace(_surfaceId))
                _surfaceId = UnityEditor.GUID.Generate().ToString();
        }
#endif
    }
}
