using System;
using Base.Localization.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Localization.View
{
    [AddComponentMenu("Tools/LocalizationText")]
    public class LocalizationText : MonoBehaviour
    {
#pragma warning disable 0649
        [SerializeField] private string _id;

        private LocalizationService _localizationService;

        private Text _textComponent;
        private TextMeshProUGUI _textMeshComponent;

        private Func<LocalizationService, string> _customFormatter;

        public string Id
        {
            get => _id;
            set
            {
                _id = value;
                _customFormatter = null;

                Refresh();
            }
        }

        [Inject]
        private void Construct(SignalBus signalBus, LocalizationService localizationService)
        {
            _localizationService = localizationService;

            _textComponent = GetComponent<Text>();
            _textMeshComponent = GetComponent<TextMeshProUGUI>();

            _localizationService.OnLanguageChanged += Refresh;

            Refresh();
        }

        public void SetFormat(string formatLocalizationId, params object[] parameters)
        {
            SetCustom(localizationService =>
            {
                string text = localizationService.GetText(formatLocalizationId);

                try
                {
                    return string.Format(text, parameters);
                }
                catch
                {
                    return text;
                }
            });
        }

        public void SetCustom(Func<LocalizationService, string> customFormatter)
        {
            _customFormatter = customFormatter;
            Refresh();
        }

        public void Clear()
        {
            if (_textMeshComponent != null)
                _textMeshComponent.text = String.Empty;
        }

        private void Refresh()
        {
            string text = _customFormatter != null
                ? _customFormatter(_localizationService)
                : _localizationService.GetText(_id);

            if (string.IsNullOrEmpty(text))
                return;

            text = text.Replace("<br>", "\n");
            if (_textComponent != null)
                _textComponent.text = text;

            if (_textMeshComponent != null)
                _textMeshComponent.text = text;
        }

        private void OnDestroy()
        {
            _localizationService.OnLanguageChanged -= Refresh;
        }
    }
}