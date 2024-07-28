using DG.Tweening;
using Pool;
using UnityEngine;
using Utils.Extensions;

namespace EffectSystem.Base
{
    public abstract class BaseEffect : BasePoolable, IEffect
    {
        private Sequence _sequence;

        public void SetHolder(Transform parent)
        {
            transform.SetParent(parent, false);
        }

        public virtual void Initialize()
        {
        }

        public void DoAnimation(System.Action onEnd)
        {
            _sequence = CreateSequence();
            _sequence.OnKill(onEnd.Invoke);
        }

        protected abstract Sequence CreateSequence();

        public override void OnDespawn(Transform parent)
        {
            _sequence?.Stop();
            _sequence = null;

            base.OnDespawn(parent);
        }
    }
}
