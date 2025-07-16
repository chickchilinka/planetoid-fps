using UnityEngine;

namespace Base.ThirdPersonCamera.Providers
{
    public interface ICameraTransformProvider
    {
        Vector3 YawPivot { get; }
        Vector3 YawUp { get; }
    }
}