using EffectSystem.Data;
using UnityEngine;

namespace EffectSystem
{
    public class EffectsSignals
    {
        public class AddHolder
        {
            public Transform Transform { get; }
            public EffectLayer EffectLayer { get; }

            public AddHolder(Transform transform, EffectLayer effectLayer)
            {
                Transform = transform;
                EffectLayer = effectLayer;
            }
        }

        public class RemoveHolder
        {
            public EffectLayer EffectLayer { get; }

            public RemoveHolder(EffectLayer effectLayer)
            {
                EffectLayer = effectLayer;
            }
        }
    }
}