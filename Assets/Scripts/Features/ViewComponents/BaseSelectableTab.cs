using System;
using Pool;
using UnityEngine;
using UnityEngine.UI;
using ViewSystem.Group;

namespace Utils.View
{
    public class BaseSelectableTab : BasePoolable, ISelectable
    {
#pragma warning disable 0649
        [SerializeField] private Button _tabButton;
        [SerializeField] private Image _tabButtonIcon;
        [SerializeField] private Transform _holder;

        [SerializeField] private Sprite _onTabSprite;
        [SerializeField] private Sprite _offTabSprite;

        public event Action Trigger;
        public bool CanBeSelected => true;

        private void Awake()
        {
            if (_onTabSprite == null)
                _onTabSprite = _tabButton.image.sprite;

            if (_offTabSprite == null)
                _offTabSprite = _tabButton.image.sprite;
        }

        public override void OnSpawn(Transform parent)
        {
            _tabButton.onClick.AddListener(OnButtonTriggered);

            base.OnSpawn(parent);
        }

        public void InitializeButtonView(float index)
        {
            _tabButton.image.rectTransform.anchoredPosition3D =
                new Vector3(_tabButton.image.rectTransform.rect.width * index,
                    _tabButton.image.rectTransform.anchoredPosition.y, 0);
        }

        public override void OnDespawn(Transform parent)
        {
            _tabButton.onClick.RemoveListener(OnButtonTriggered);

            base.OnDespawn(parent);
        }

        public void SetSelection(bool isSelected)
        {
            _holder.gameObject.SetActive(isSelected);
            _tabButton.image.sprite = isSelected ? _onTabSprite : _offTabSprite;
        }

        protected void SetTabIcon(Sprite sprite)
        {
            _tabButtonIcon.sprite = sprite;
        }

        private void OnButtonTriggered()
        {
            Trigger?.Invoke();
        }
    }
}