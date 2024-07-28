using UnityEngine;

namespace Utils.View
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
 
            GUIStyle style = new GUIStyle();
 
            float msec = _deltaTime * 1000.0f;
            float fps = 1.0f / _deltaTime;
            
            Rect rect = new Rect(0, 140, w, h * 2 / 100);
            style.alignment = TextAnchor.UpperLeft;
            style.fontSize = h * 2 / 100;
            style.normal.textColor = GetColor(fps);
            
            string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
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