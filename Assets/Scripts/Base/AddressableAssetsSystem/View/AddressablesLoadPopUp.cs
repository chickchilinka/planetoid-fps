using System;
using UnityEngine;
using UnityEngine.UI;
using ViewSystem;
using ViewSystem.Base;

namespace Addressables.View
{
    public class AddressablesLoadPopUpData : IViewInput
    {
        public Action OkCallback { get; }

        public AddressablesLoadPopUpData(Action okCallback)
        {
            OkCallback = okCallback;
        }
    }
    
    public class AddressablesLoadPopUp : AnimatedPopUpWithInputView<AddressablesLoadPopUpData>
    {
#pragma warning disable 0649
        [Space]
        [SerializeField] private Button _okButton;
        
        public override ViewLayer ViewLayer => ViewLayer.Global;

        private Action _callback;
        
        public override void Show(AddressablesLoadPopUpData data)
        {
            _callback = data.OkCallback;
            _okButton.onClick.AddListener(FireCallback);
            base.Show(data);
        }

        public override void Hide(Action onEndHiding)
        {
            _okButton.onClick.RemoveListener(FireCallback);
            base.Hide(onEndHiding);
        }

        private void FireCallback()
        {
            _callback?.Invoke();
            _callback = null;
        }
    }
}