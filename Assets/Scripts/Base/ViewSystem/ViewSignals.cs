using UnityEngine;

namespace ViewSystem
{
    public class ViewSignals
    {
        public class AddHolder
        {
            public Transform Transform { get; }
            public ViewLayer ViewLayer { get; }

            public AddHolder(Transform transform, ViewLayer viewLayer)
            {
                Transform = transform;
                ViewLayer = viewLayer;
            }
        }
        
        public class BaseActivitySignal
        {
            public string ViewName { get; }

            public BaseActivitySignal(string viewName)
            {
                ViewName = viewName;
            }

            public bool IsEqual<TClass>() where TClass : class
            {
                return typeof(TClass).Name.Equals(ViewName);
            }
        }
        
        public class Shown : BaseActivitySignal
        {
            public Shown(string viewName) : base(viewName)
            {
            }
        }

        public class Hidden : BaseActivitySignal
        {
            public bool AllWindowsAreClosed { get; }
            
            public Hidden(string viewName, bool allWindowsAreClosed) : base(viewName)
            {
                AllWindowsAreClosed = allWindowsAreClosed;
            }
        }
    }
}
