using System.Collections.Generic;
using CubeTower.Runtime.Config;
using DG.Tweening;
using TMPro;
using UnityEngine;
using Zenject;

namespace CubeTower.Runtime.UI
{
    /// <summary>
    /// Feedback label above the bottom panel. A new message replaces the current one.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class MessageView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _content;

        private MessageSettings _settings;
        private Sequence _sequence;

        [Inject]
        public void Construct(GameplaySettings settings)
        {
            _settings = settings.Messages;
        }

        private void Awake()
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }

        /// <summary>
        /// Renders glyphs of the given texts into the dynamic font atlas in advance,
        /// so the first message in a new script (e.g. Cyrillic) doesn't cause a frame hitch.
        /// </summary>
        public void Prewarm(IEnumerable<string> texts)
        {
            var font = _label.font;
            if (font == null)
                return;

            foreach (var text in texts)
            {
                if (!string.IsNullOrEmpty(text))
                    font.HasCharacters(text, out uint[] _, searchFallbacks: true, tryAddCharacter: true);
            }
        }

        public void Show(string text)
        {
            _sequence?.Kill();
            _label.text = text;
            _content.localScale = Vector3.one * 0.85f;

            _sequence = DOTween.Sequence()
                .Append(_canvasGroup.DOFade(1f, _settings.FadeDuration))
                .Join(_content.DOScale(1f, _settings.FadeDuration * 1.5f).SetEase(Ease.OutBack))
                .AppendInterval(_settings.VisibleDuration)
                .Append(_canvasGroup.DOFade(0f, _settings.FadeDuration))
                .SetLink(gameObject);
        }

        private void OnDestroy() => _sequence?.Kill();

        private void Reset()
        {
            _label = GetComponentInChildren<TMP_Text>();
            _canvasGroup = GetComponent<CanvasGroup>();
            _content = (RectTransform)transform;
        }
    }
}
