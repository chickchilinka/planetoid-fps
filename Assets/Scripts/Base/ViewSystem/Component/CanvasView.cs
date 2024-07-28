using UnityEngine;
using UnityEngine.UI;

namespace ViewSystem.Component
{
    public class CanvasView : MonoBehaviour
    {
        private CanvasScaler _canvasScaler;
        private CanvasScaler CanvasScaler => _canvasScaler ?? (_canvasScaler = GetComponent<CanvasScaler>());
        
        private const float RatioMatchByHeight = 1.93f;
        private const float RatioMatchByWidth = 1.33f;
        
        private const float WidthMatch = 0f;
        private const float HeightMatch = 1f;
        private const float MiddleMatch = 0.5f;
        
        private void Awake()
        {
            var screenRatio = Screen.height / (float)Screen.width;
            
            if (screenRatio >= RatioMatchByHeight)
                CanvasScaler.matchWidthOrHeight = WidthMatch;
            else if (screenRatio <= RatioMatchByWidth)
                CanvasScaler.matchWidthOrHeight = HeightMatch;
            else
                CanvasScaler.matchWidthOrHeight = MiddleMatch;
        }
    }
}
