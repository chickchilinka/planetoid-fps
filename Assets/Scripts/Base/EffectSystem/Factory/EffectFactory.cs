using Base.EffectSystem.Base;
using Base.EffectSystem.Data;
using Base.Pool.Services;
using EffectSystem.Storage;
using UnityEngine;

namespace Base.EffectSystem.Factory
{
    public class EffectFactory
    {
        private readonly Transform _inactiveEffectContainer;
        private readonly GeneralPool _generalPool;
        private readonly EffectsHolderStorage _effectsHolderStorage;

        public EffectFactory(GeneralPool generalPool, EffectsHolderStorage effectsHolderStorage)
        {
            _generalPool = generalPool;
            _effectsHolderStorage = effectsHolderStorage;

            _inactiveEffectContainer = new GameObject(GetType().FullName).transform;
            
            Object.DontDestroyOnLoad(_inactiveEffectContainer.gameObject);
        }

        public IEffect Spawn<TEffect>(EffectLayer effectLayer) where TEffect : IEffect
        {
            return Spawn(typeof(TEffect).FullName, effectLayer);
        }
        
        public virtual IEffect Spawn(string effectId, EffectLayer effectLayer)
        {
            var effect = _generalPool.Spawn<IEffect>(_inactiveEffectContainer, effectId);

            if (effect == null)
            {
                Debug.LogError("There is no effect with id " + effectId);
                return null;
            }
            
            var holder = GetHolder(effectLayer);
            effect.SetHolder(holder);

            return effect;
        }

        public void Despawn(IEffect effect)
        {
            _generalPool.Despawn(effect);
        }

        private Transform GetHolder(EffectLayer layer)
        {
            if (!_effectsHolderStorage.TryGetEffectsHolder(layer, out var holder))
            {
                Debug.LogError($"Holder {layer} was not found");
                return null;
            }
            
            return holder.transform;
        }
    }
}
