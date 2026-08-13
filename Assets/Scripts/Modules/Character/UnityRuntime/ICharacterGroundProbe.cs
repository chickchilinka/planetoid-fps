using UnityEngine;

namespace Modules.Character.UnityRuntime
{
    public interface ICharacterGroundProbe
    {
        bool IsGrounded(Vector3 position, Vector3 up, out Vector3 normal);
    }
}
