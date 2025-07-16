using UnityEngine;

namespace Features.ViewComponents
{
    public class FPSDisplay : MonoBehaviour
    {
        private float _deltaTime;
        
        private readonly Color _orange = new Color(1f, 0.6f, 0f, 1f);
        private readonly float _minFps = 15;
        private readonly float _middleFps = 25;
        
        private void OnGUI()
        {
            int w = Screen.width, h = Screen.height;
 
            var style = new GUIStyle();
 
            var msec = _deltaTime * 1000.0f;
            var fps = 1.0f / _deltaTime;
            
            var rect = new Rect(0, 140, w, h * 2 / 100);
            style.alignment = TextAnchor.UpperLeft;
            style.fontSize = h * 2 / 100;
            style.normal.textColor = GetColor(fps);
            
            var text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
            GUI.Label(rect, text, style);
        }

        private Color GetColor(float fps)
        {
            if (fps < _minFps)
                return Color.red;

            if (fps >= _minFps && fps < _middleFps)
                return _orange;
            
            return Color.green;
        }
        
        private void Update()
        {
            _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
        }
    }
}