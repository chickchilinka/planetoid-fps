using System.Collections.Generic;
using UnityEngine;

namespace Utils.View
{
    public class TapData
    {
        private readonly int _number;
        
        public float CreateTime { get; }
        public Rect Rect { get; }

        public string Label => _number.ToString();
        
        public TapData(int number, float createTime, Rect rect)
        {
            _number = number;
            CreateTime = createTime;
            Rect = rect;
        }
    }
    
    public class TapDisplay : MonoBehaviour
    {
        private const float DelaySecond = 3;
        private const int DefaultNumber = 1;

        private GUIStyle _style;
        private int _number = DefaultNumber;

        private readonly List<TapData> _tapDataList = new List<TapData>();

        private void Awake()
        {
            _style = new GUIStyle()
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 40,
                normal = new GUIStyleState{textColor = Color.cyan}
            };
        }

        private void OnGUI()
        {
            var tapDataArray = _tapDataList.ToArray();
            foreach (var tapData in tapDataArray)
                GUI.Label(tapData.Rect, tapData.Label, _style);
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _tapDataList.Add(new TapData(_number, Time.realtimeSinceStartup, GetMouseRect()));
                _number++;
            }

            _tapDataList.RemoveAll(tapData => tapData.CreateTime + DelaySecond < Time.realtimeSinceStartup);

            if (_tapDataList.Count == 0)
                _number = DefaultNumber;
        }

        private Rect GetMouseRect()
        {
            var position = Input.mousePosition;
            return new Rect(position.x, Screen.height - position.y, 0, 0);
        }
    }
}