using UnityEngine;
using Zenject;

namespace ViewSystem
{
    public class ViewHolder : MonoBehaviour
    {
#pragma warning disable 0649
        [SerializeField] private ViewLayer _viewLayer;
        
        [Inject]
        public void Construct(SignalBus signalBus)
        {
            signalBus.Fire(new ViewSignals.AddHolder(transform, _viewLayer));
        }
    }
}
