using System.Collections.Generic;
using Base.EffectSystem.Data;
using EffectSystem.View;
using UnityEngine;

namespace EffectSystem.Storage
{
    public class EffectsHolderStorage
    {
        protected readonly Dictionary<EffectLayer, EffectsHolder> _holders = new Dictionary<EffectLayer, EffectsHolder>();

        public void AddEffectsHolder(EffectLayer effectLayer, EffectsHolder effectsHolder)
        {
            _holders.TryAdd(effectLayer, effectsHolder);
        }

        public void RemoveEffectsHolder(EffectLayer effectLayer)
        {
            if(_holders.ContainsKey(effectLayer))
                _holders.Remove(effectLayer);
        }

        public bool TryGetEffectsHolder(EffectLayer effectLayer, out EffectsHolder effectsHolder)
        {
            return _holders.TryGetValue(effectLayer, out effectsHolder);
        }
    }
}