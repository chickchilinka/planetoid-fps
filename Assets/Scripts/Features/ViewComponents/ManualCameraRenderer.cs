using System;
using UniRx;
using UnityEngine;


[RequireComponent(typeof(Camera))]
public class ManualCameraRenderer : MonoBehaviour
{
    [SerializeField] private int _fps = 20;

    private float _elapsed;
    private Camera _camera;

    private IDisposable _disposable;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        _camera.enabled = false;
    }

    private void OnEnable()
    {
        _camera?.Render();

        _disposable = Observable.EveryUpdate()
            .Subscribe(_ =>
            {
                _elapsed += Time.deltaTime;

                if (_elapsed > 1f / _fps)
                {
                    _elapsed = 0;
                    _camera.Render();
                }
            });
    }

    private void OnDisable()
    {
        _disposable?.Dispose();
    }
}