// MarrowMachine CONFIDENTIAL
// __________________
//
// [2016] - [2024] MarrowMachine LLC
// All Rights Reserved.
//
// NOTICE:  All information contained herein is, and remains
// the property of MarrowMachine LLC and its suppliers,
// if any.  The intellectual and technical concepts contained
// herein are proprietary to MarrowMachine LLC
// and its suppliers and may be covered by U.S. and Foreign Patents,
// patents in process, and are protected by trade secret or copyright law.
// Dissemination of this information or reproduction of this material
// is strictly forbidden unless prior written permission is obtained
// from MarrowMachine LLC.

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