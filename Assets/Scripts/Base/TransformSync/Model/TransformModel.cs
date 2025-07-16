using System;
using UniRx;
using UnityEngine;

namespace Base.Transformation.Model
{
    public class TransformModel: IIdentified, IDisposable
    {
        public string Id { get; }
        
        public IReadOnlyReactiveProperty<Vector3> Position => _position;
        public IReadOnlyReactiveProperty<Quaternion> Rotation => _rotation;
        public IReadOnlyReactiveProperty<Vector3> Scale => _scale;
        
        private ReactiveProperty<Vector3> _position = new ReactiveProperty<Vector3>();
        private ReactiveProperty<Quaternion> _rotation = new ReactiveProperty<Quaternion>();
        private ReactiveProperty<Vector3> _scale = new ReactiveProperty<Vector3>();
        
        public TransformModel(string id)
        {
            Id = id;
        }

        public void SetPosition(float x, float y, float z)
        {
            _position.Value = new Vector3(x, y, z);
        }

        public void SetPosition(Vector3 position)
        {
            _position.Value = position;
        }

        public void SetRotation(Quaternion rotation)
        {
            _rotation.Value = rotation;
        }

        public void SetScale(Vector3 scale)
        {
            _scale.Value = scale;
        }

        public void Dispose()
        {
            _position?.Dispose();
            _rotation?.Dispose();
            _scale?.Dispose();
        }
    }
}