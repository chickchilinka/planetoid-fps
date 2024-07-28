using System.Collections.Generic;
using EffectSystem.Base;
using EffectSystem.Data;
using Pool;
using UnityEngine;
using Zenject;

namespace EffectSystem
{
    public class EffectFactory
    {
        protected readonly Transform _inactiveEffectContainer;
        protected readonly GeneralPool _generalPool;

        protected readonly Dictionary<EffectLayer, Transform> _holders = new Dictionary<EffectLayer, Transform>();

        public EffectFactory(GeneralPool generalPool, SignalBus signalBus)
        {
            _generalPool = generalPool;

            _inactiveEffectContainer = new GameObject(GetType().FullName).transform;
            
            Object.DontDestroyOnLoad(_inactiveEffectContainer.gameObject);
            
            signalBus.Subscribe<EffectsSignals.AddHolder>(signal => AddHolder(signal.Transform, signal.EffectLayer));
            signalBus.Subscribe<EffectsSignals.RemoveHolder>(signal => RemoveHolder(signal.EffectLayer));
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

        protected Transform GetHolder(EffectLayer layer)
        {
            if (!_holders.ContainsKey(layer))
            {
                Debug.LogError($"Holder {layer} was not found");
            }
            
            return _holders[layer];
        }
        
        private void AddHolder(Transform transform, EffectLayer effectLayer)
        {
            if (!_holders.ContainsKey(effectLayer))
                _holders.Add(effectLayer, null);

            _holders[effectLayer] = transform;
        }

        private void RemoveHolder(EffectLayer effectLayer)
        {
            if (_holders.ContainsKey(effectLayer))
                _holders.Remove(effectLayer);
        }
    }
}
