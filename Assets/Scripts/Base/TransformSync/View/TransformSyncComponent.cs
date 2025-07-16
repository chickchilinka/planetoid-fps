using Base.Transformation.Data;
using Base.Transformation.Model;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject;

namespace Base.Transformation.View
{
    public class TransformSyncComponent : MonoBehaviour
    {
        [FormerlySerializedAs("_transformMode")] [SerializeField] 
        private TransformSyncMode _transformSyncMode;
        public TransformSyncMode TransformSyncMode => _transformSyncMode;

        [Inject]
        public void Construct()
        {
            
        }

        public void Initialize(string id)
        {
            
        }
    }
}