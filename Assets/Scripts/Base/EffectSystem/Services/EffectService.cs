using System;
using System.Collections.Generic;
using System.Linq;
using Base.EffectSystem.Base;
using Base.EffectSystem.Data;
using Base.EffectSystem.Factory;
using EffectSystem;
using Utils.Extensions;

namespace Base.EffectSystem.Services
{
    public class EffectService
    {
        public event Action OnAllEffectsCompleted; 
        private readonly EffectFactory _effectFactory;
        private readonly List<IEffect> _activeEffects = new List<IEffect>();

        protected EffectService(EffectFactory effectFactory)
        {
            _effectFactory = effectFactory;
        }

        public void ShowEffect<TEffect>(EffectLayer effectLayer) where TEffect : BaseEffect
        {
            ShowEffect(typeof(TEffect).Name, effectLayer);
        }
        
        public void ShowEffect(string effectId, EffectLayer effectLayer)
        {
            var effect = SpawnEffect(effectId, effectLayer);
            if (effect == null)
                return;

            AddEffect(effect as BaseEffect);
            effect.Initialize();
        }

        public void ShowEffectWithInput<TEffect, TInput>(TInput input, EffectLayer effectLayer)
            where TInput : IEffectData
            where TEffect : BaseEffectWithInputData<TInput>
        {
            ShowEffectWithInput(typeof(TEffect).Name, input, effectLayer);
        }
        
        public void ShowEffectWithInput<TInput>(string effectId, TInput input, EffectLayer effectLayer)
            where TInput : IEffectData
        {
            var effect = SpawnEffect(effectId, effectLayer) as IEffectWithInputData<TInput>;

            if (effect == null)
                return;

            effect.Initialize(input);
            AddEffect(effect as BaseEffect);
        }

        public void FinishEffectsByType<TEffect>() where TEffect : BaseEffect
        {
            var effect = _activeEffects.FirstOrDefault(tEffect => tEffect is TEffect);
            if (effect != null)
            {
                OnFinishEffect((BaseEffect)effect);
            }
        }

        public IEffect GetLastEffect()
        {
            if (_activeEffects.IsNullOrEmpty())
                return null;

            return _activeEffects.Last();
        }

        public int CountActiveEffects()
        {
            return _activeEffects.Count;
        }

        public bool IsAnyEffectActive()
        {
            return _activeEffects.Any();
        }

        private IEffect SpawnEffect(string effectId, EffectLayer effectLayer)
        {
            return _effectFactory.Spawn(effectId, effectLayer);
        }

        private void AddEffect(BaseEffect effect)
        {
            _activeEffects.Add(effect);
            effect.DoAnimation(() => OnFinishEffect(effect));
        }

        private void OnFinishEffect(IEffect effect)
        {
            if (!_activeEffects.Contains(effect))
                return;
            
            _activeEffects.Remove(effect);
            _effectFactory.Despawn(effect);
            if(!_activeEffects.Any())
                OnAllEffectsCompleted?.Invoke();
        }

        public void ResetActiveEffects()
        {
            while(_activeEffects.Count > 0)
                OnFinishEffect(_activeEffects[0]);

            _activeEffects.Clear();
        }
    }
}