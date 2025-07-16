using System;
using Base.Transformation.Model;
using UnityEngine;

namespace Base.Transformation
{
    public interface ITransformController: IDisposable
    {
        public void Initialize(TransformModel model, GameObject gameObject);
    }
}