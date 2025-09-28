using UnityEngine;
using DG.Tweening;
using Spine;
using Spine.Unity;

namespace Scene.View
{
    public class SpineProgressView : MonoBehaviour
    {
#pragma warning disable 0649

        [SerializeField] private SkeletonGraphic _skeletonGraphic;
        [SerializeField] private float _finishProgressAnimationDuration;

        private float _animDuration;
        private string _animName;

        public bool IsAnimationPlaying { get; private set; }

        private TrackEntry _track;
        private float _newProgress;

        private Tween _tween;
        
        public void Initialize()
        {
            _tween?.Kill();
            _newProgress = 0;

            _skeletonGraphic.AnimationState.ClearTrack(0);
            _skeletonGraphic.Skeleton.SetToSetupPose();

            IsAnimationPlaying = true;

            _animDuration = _skeletonGraphic.SkeletonData.Animations.Items[0].Duration - _finishProgressAnimationDuration;
            _animName = _skeletonGraphic.SkeletonData.Animations.Items[0].Name;

            _track = _skeletonGraphic.AnimationState.SetAnimation(0, _animName, false);
            _track.TimeScale = 0f;
            _track.TrackTime = 0f;
        }

        public void ChangeProgress(float newProgress, float duration)
        {
            _tween?.Kill();

            if (Mathf.Approximately(newProgress, _newProgress) && _tween != null && _tween.IsPlaying())
                return;

            _tween = DOTween.To(value => _track.TrackTime = value, _track.TrackTime, newProgress * _animDuration, duration);

            _newProgress = newProgress;
        }

        public void PlayFinishAnimation()
        {
            _track.TimeScale = 1f;
        }
    }
}