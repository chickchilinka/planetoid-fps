using UnityEngine;

namespace ViewSystem.Button
{
    [RequireComponent(typeof(UnityEngine.UI.Button))]
    public abstract class AbstractButton : MonoBehaviour
    {
        private UnityEngine.UI.Button _button;
        protected UnityEngine.UI.Button Button => _button ?? (_button = GetComponent<UnityEngine.UI.Button>());
        
        public bool IsManualActivation { get; set; }

        public bool IsInteractable
        {
            get => Button.interactable;
            set => Button.interactable = value;
        }
        
        protected virtual void Awake()
        {
            Button.onClick.AddListener(OnButtonClick);
        }

        public abstract void Activate();

        protected virtual void OnButtonClick()
        {
            if (!IsManualActivation)
                Activate();
        }
    }
}
