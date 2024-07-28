using System;
using Pool;
using UnityEngine;

namespace EffectSystem.Base
{
    public interface IEffect : IPoolable
    {
        void Initialize();
        void DoAnimation(Action onEnd);

        void SetHolder(Transform parent);
    }
}
