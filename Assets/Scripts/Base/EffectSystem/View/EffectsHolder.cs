using EffectSystem.Data;
using UnityEngine;
using Zenject;

namespace EffectSystem.View
{
    public class EffectsHolder : MonoBehaviour
    {
#pragma warning disable 0649
        [SerializeField] private EffectLayer _effectLayer;
        private SignalBus _signalBus;

        [Inject]
        public void Construct(SignalBus signalBus)
        {
            _signalBus = signalBus;
            _signalBus.Fire(new EffectsSignals.AddHolder(transform, _effectLayer));
        }

        private void OnDestroy()
        {
            _signalBus?.Fire(new EffectsSignals.RemoveHolder(_effectLayer));
        }
    }
}
