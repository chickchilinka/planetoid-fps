using Pool;
using UnityEngine;

namespace Utils.View
{
    public class FullScreenHolder : BasePoolable
    {
        private RectTransform _rectTransform;
        public RectTransform RectTransform => _rectTransform ?? (_rectTransform = GetComponent<RectTransform>());
    }
}