using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Base.ThirdPersonCamera.Data
{
    [Serializable]
    public class ThirdPersonCameraSettings
    {
        public float FollowSpeed = 5f;
        public float YawFollowSpeed = 5f;
        public bool InvertY = true;
        public float DefaultSensitivity = 1f;
        public float MaxPitch = 75;
        public float MinPitch = -75;
    }
}