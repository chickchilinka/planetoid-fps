using System;
using UnityEngine;

namespace Base.ThirdPersonCamera.Providers
{
    public interface ICameraInputProvider
    {
        Vector2 LookDelta { get; }
    }
}