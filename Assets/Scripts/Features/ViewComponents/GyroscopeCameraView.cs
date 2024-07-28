using System;
using UniRx;
using UnityEngine;

public class GyroscopeCameraView : MonoBehaviour
{
    [SerializeField] private Vector3 _range = new Vector3(2, 4, 0);
    [SerializeField] private Vector3 _speed = new Vector3(-5, -10, 0);

    private IDisposable _disposable;
    private Gyroscope _gyroscope;

    private Vector3 _baseRotation;
    private Vector3 _offset;

    private void Awake()
    {
        _baseRotation = transform.localRotation.eulerAngles;

        if (SystemInfo.supportsGyroscope)
        {
            _gyroscope = Input.gyro;
        }
    }

    private void OnEnable()
    {
        if (_gyroscope == null)
            return;

        transform.localRotation = Quaternion.Euler(_baseRotation);

        _gyroscope.enabled = true;
        _offset = Vector3.zero;

        _disposable = Observable.EveryUpdate()
             .Subscribe(_ =>
             {
                 var offset = _gyroscope.rotationRateUnbiased * Time.deltaTime;
                 offset.Scale(_speed);

                 _offset += offset;

                 _offset.x = Mathf.Clamp(_offset.x, -_range.x, _range.y);
                 _offset.y = Mathf.Clamp(_offset.y, -_range.y, _range.y);
                 _offset.z = Mathf.Clamp(_offset.z, -_range.z, _range.z);

                 transform.localRotation = Quaternion.Euler(_baseRotation + _offset);
             });
    }

    private void OnDisable()
    {
        if (_gyroscope == null)
            return;

        _gyroscope.enabled = false;

        _disposable?.Dispose();
    }
}