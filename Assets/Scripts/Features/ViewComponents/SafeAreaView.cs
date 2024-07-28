using System;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Utils.Extensions;

namespace Utils.View
{
    public class SafeAreaView : MonoBehaviour 
    {
#pragma warning disable 0649
        [SerializeField] private Image _topFrame;
        [SerializeField] private Image _bottomFrame;
        
        private readonly float _timeInterval = 0.5f;
        private DeviceOrientation _deviceOrientation;
        private RectTransform _rectTransform;

        private void Awake()
        {
            if (_rectTransform == null)
                _rectTransform = transform as RectTransform;

            Refresh();

            Observable.Timer(TimeSpan.FromSeconds(_timeInterval))
                .Repeat()
                .Subscribe(_ =>
                {
                    if (_deviceOrientation != Input.deviceOrientation) 
                        Refresh();
                })
                .AddTo(this);
        }

        private void Refresh()
        {
            _deviceOrientation = Input.deviceOrientation;

            var safeArea = UIExtentions.GetSafeArea();

            var anchorMin = safeArea.position;
            var anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            _rectTransform.anchorMin = anchorMin;
            _rectTransform.anchorMax = anchorMax;

            if (_bottomFrame != null)
            {
                _bottomFrame.rectTransform.anchorMin = Vector2.zero;
                _bottomFrame.rectTransform.anchorMax = new Vector2(1, anchorMin.y);
            }

            if (_topFrame != null)
            {
                _topFrame.rectTransform.anchorMin = new Vector2(0, anchorMax.y);
                _topFrame.rectTransform.anchorMax = Vector2.one;
            }
        }
    }
}