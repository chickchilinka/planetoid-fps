using Base.EffectSystem.Data;
using EffectSystem.Storage;
using UnityEngine;
using Zenject;

namespace EffectSystem.View
{
    public class EffectsHolder : MonoBehaviour
    {
        [SerializeField] private EffectLayer _effectLayer;
        private EffectsHolderStorage _storage;

        [Inject]
        public void Construct(EffectsHolderStorage storage)
        {
            _storage = storage;
            _storage.AddEffectsHolder(_effectLayer, this);
        }

        private void OnDestroy()
        {
            _storage.RemoveEffectsHolder(_effectLayer);
        }
    }
}
