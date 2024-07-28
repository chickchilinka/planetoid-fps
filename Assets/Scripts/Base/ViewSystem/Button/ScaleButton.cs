using UnityEngine;
using ViewSystem.Button.Animation;

namespace ViewSystem.Button
{
    public class ScaleButton : BaseButton<ButtonScaleAnimation>
    {
#pragma warning disable 0649
        [SerializeField] private Transform _animationTransform;
        
        protected override void Awake()
        {
            base.Awake();
            Animation = new ButtonScaleAnimation(_animationTransform);
        }
    }
}
