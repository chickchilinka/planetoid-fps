using System;
using Pool;
using UnityEngine;

namespace ViewSystem.Base
{
    public abstract class BaseView : BasePoolable, IView
    {
        
        public abstract ViewLayer ViewLayer { get; }
        
        public bool IsShown { get; protected set; }

        public bool IsValid => this != null && transform != null;

        public void Initialize(Transform parent)
        {
            if (parent != null)
            {
                if (!IsValid)
                {
                    Debug.LogError($"Attempt to initialize null view with type {GetType().Name}");
                    return;
                }

                transform.SetParent(parent, false);
            }
        }

        public virtual void Show()
        {
            IsShown = true;
            if(gameObject!=null)
                gameObject.SetActive(true);
            if(transform!=null)
                transform.SetAsLastSibling();
        }

        public virtual void Hide(Action onEndHiding)
        {
            gameObject?.SetActive(false);
            IsShown = false;
            
            onEndHiding?.Invoke();
        }
    }
}
