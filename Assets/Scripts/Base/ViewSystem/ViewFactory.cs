using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ViewSystem.Base;
using Zenject;

namespace ViewSystem
{
    public class ViewFactory
    {
        private readonly DiContainer _container;
        private readonly Transform _inactiveViewContainer;

        private readonly Dictionary<ViewLayer, Transform> _holders = new Dictionary<ViewLayer, Transform>();
        private readonly Dictionary<Type, IView> _inactiveViews = new Dictionary<Type, IView>();
        private readonly List<IView> _activeViews = new List<IView>();
        
        private readonly BaseView[] _viewPrefabs;
        private const string ResourcesPrefabsWindow = "Prefabs/Window/";
        
        public int ActiveViewsCount => _activeViews.Count;

        public ViewFactory(DiContainer container, SignalBus signalBus)
        {
            _container = container;
            _viewPrefabs = Resources.LoadAll<BaseView>(ResourcesPrefabsWindow);
            
            _inactiveViewContainer = new GameObject(GetType().Name).transform;
            GameObject.DontDestroyOnLoad(_inactiveViewContainer.gameObject);
            
            signalBus.Subscribe<ViewSignals.AddHolder>(signal => AddHolder(signal.Transform, signal.ViewLayer));
        }
        
        public IView GetView(Type type)
        {
            if (_inactiveViews.ContainsKey(type))
                return _inactiveViews[type];
            
            var view = Spawn<IView>(type);
            
            _inactiveViews.Add(type, view);

            return view;
        }

        public void ActivateView(IView view)
        {
            if (view == null || !view.IsValid)
                return;
            
            view.Initialize(GetHolder(view));
            
            _activeViews.Add(view);
            _inactiveViews.Remove(view.GetType());
        }
        
        public void DeactivateView(IView view)
        {
            if (view == null)
                return;
            
            if (_inactiveViewContainer != null)
                view.Initialize(_inactiveViewContainer);

            _activeViews.Remove(view);
            
            if (!_inactiveViews.ContainsKey(view.GetType()) && view.IsValid)
                _inactiveViews.Add(view.GetType(), view);
        }
        
        public IView GetLastOrDefaultActiveView()
        {
            return _activeViews.LastOrDefault();
        }

        public bool IsViewActivated(Type type)
        {
            return GetActivatedView(type) != null;
        }
        
        public IView GetActivatedView(Type type)
        {
            foreach (var activeView in _activeViews)
            {
                if (activeView.GetType() == type)
                    return activeView;
            }

            return null;
        }

        private TView Spawn<TView>(Type type) where TView : class, IView
        {
            var prefab = _viewPrefabs.FirstOrDefault(w => w.GetType().Name.Equals(type.Name));
            if (prefab == null)
            {
                Debug.LogWarning("[ViewFactory] can't find view for type : " + type);
                return null;
            }
            var view = _container.InstantiatePrefabForComponent<TView>(prefab.gameObject);
            
            if (view == null)
            {
                Debug.LogError("There is no view with type " + type.Name);
                return null;
            }
            
            return view;
        }

        private Transform GetHolder(IView view)
        {
            if (!_holders.ContainsKey(view.ViewLayer))
            {
                Debug.LogError($"Holder {view.ViewLayer} for {view.GetType().Name} was not found");
            }
            
            return _holders[view.ViewLayer];
        }
        
        private void AddHolder(Transform transform, ViewLayer viewLayer)
        {
//            Debug.Log("Add holder " + viewLayer);
            if (!_holders.ContainsKey(viewLayer))
                _holders.Add(viewLayer, null);

            _holders[viewLayer] = transform;
        }
    }
}
