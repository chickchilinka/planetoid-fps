using UnityEngine;
using UnityEngine.EventSystems;
using System;
using UniRx;

namespace View
{
    public class ScrollButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private float _speed;

        public event Action<float> OnPressing = delegate { };

        private IDisposable _stream;


        public void OnPointerDown(PointerEventData eventData)
        {
            _stream = Observable.EveryUpdate()
                        .Subscribe(x => OnPressing(Time.deltaTime * _speed));
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _stream?.Dispose();
        }

        private void OnDisable()
        {
            _stream?.Dispose();
        }
    }
}