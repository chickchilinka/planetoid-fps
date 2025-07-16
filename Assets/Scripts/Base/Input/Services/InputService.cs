
using UnityEngine;

namespace Base.Input.Services
{
    public class InputService
    {
        public float GetAxis(string axis)
        {
            return UnityEngine.Input.GetAxis(axis);
        }

        public bool GetButton(string button)
        {
            return UnityEngine.Input.GetButton(button);
        }

        public Vector2 GetMouseDelta()
        {
            return new(UnityEngine.Input.GetAxis("Mouse X"), UnityEngine.Input.GetAxis("Mouse Y"));
        }
    }
}