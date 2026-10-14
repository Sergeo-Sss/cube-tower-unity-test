using System.Collections.Generic;
using System.Threading;
using CubeTower.Core.Cubes;
using CubeTower.Runtime.Gameplay.Cubes;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace CubeTower.Runtime.Gameplay.Palette
{
    /// <summary>
    /// Bottom horizontal scroll with the cubes from the config.
    /// </summary>
    public sealed class PaletteView : MonoBehaviour
    {
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private RectTransform _content;
        [SerializeField] private HorizontalLayoutGroup _layout;

        [Tooltip("Where the scroll hint swipe starts, as a fraction of the viewport.")]
        [SerializeField] private Vector2 _scrollHintStart = new Vector2(0.7f, 0.85f);

        private readonly List<PaletteItem> _items = new List<PaletteItem>();
        private readonly Vector3[] _corners = new Vector3[4];
        private PaletteItem.Factory _factory;

        [Inject]
        public void Construct(PaletteItem.Factory factory)
        {
            _factory = factory;
        }

        private RectTransform Viewport => _scrollRect.viewport != null ? _scrollRect.viewport : (RectTransform)_scrollRect.transform;

        /// <summary>How far the content can scroll, in content units.</summary>
        public float MaxScroll => Mathf.Max(0f, _content.rect.width - Viewport.rect.width);

        /// <summary>Distance between neighbouring cubes, in content units.</summary>
        public float ItemStep => _items.Count > 0
            ? ((RectTransform)_items[0].transform).rect.width + (_layout != null ? _layout.spacing : 0f)
            : 0f;

        public Vector3 ScrollHintStart
        {
            get
            {
                var rect = Viewport.rect;
                return Viewport.TransformPoint(new Vector2(
                    rect.xMin + rect.width * _scrollHintStart.x,
                    rect.yMin + rect.height * _scrollHintStart.y));
            }
        }

        public void Build(IReadOnlyList<CubeColorId> cubes)
        {
            Clear();
            foreach (var color in cubes)
            {
                var item = _factory.Create();
                item.transform.SetParent(_content, false);
                item.Setup(color, _scrollRect);
                _items.Add(item);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
            _scrollRect.horizontalNormalizedPosition = 0f;
            ExtendTouchAreas();
        }

        /// <summary>
        /// Gaps between cubes and the free space above / below them belong to the closest cube,
        /// so a pick gesture started next to a cube still picks it instead of scrolling the panel.
        /// </summary>
        private void ExtendTouchAreas()
        {
            if (_items.Count == 0)
                return;

            var cubeRect = ((RectTransform)_items[0].transform).rect;
            var horizontal = (_layout != null ? _layout.spacing : 0f) * 0.5f;
            var vertical = Mathf.Max(0f, (Viewport.rect.height - cubeRect.height) * 0.5f);
            var padding = new Vector4(-horizontal, -vertical, -horizontal, -vertical);
            foreach (var item in _items)
                item.Cube.SetTouchPadding(padding);
        }

        public Vector3 ContentToWorldVector(Vector2 contentVector) => _content.TransformVector(contentVector);

        /// <summary>
        /// The fully visible cube closest to the viewport centre - a natural source for the hint.
        /// </summary>
        public bool TryGetHintCube(out CubeView cube)
        {
            cube = null;
            Viewport.GetWorldCorners(_corners);
            var minX = _corners[0].x;
            var maxX = _corners[2].x;
            var centerX = (minX + maxX) * 0.5f;
            var bestDistance = float.MaxValue;

            foreach (var item in _items)
            {
                ((RectTransform)item.transform).GetWorldCorners(_corners);
                if (_corners[0].x < minX || _corners[2].x > maxX)
                    continue;

                var distance = Mathf.Abs((_corners[0].x + _corners[2].x) * 0.5f - centerX);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    cube = item.Cube;
                }
            }

            return cube != null;
        }

        /// <summary>
        /// Animated scroll by <paramref name="delta"/> content units (negative - towards the end of the list).
        /// </summary>
        public UniTask ScrollByAsync(float delta, float duration, CancellationToken cancellationToken)
        {
            _scrollRect.StopMovement();
            var target = Mathf.Clamp(_content.anchoredPosition.x + delta, -MaxScroll, 0f);
            return _content.DOAnchorPosX(target, duration)
                .SetEase(Ease.InOutSine)
                .SetLink(gameObject)
                .ToUniTask(TweenCancelBehaviour.CancelAwait, cancellationToken);
        }

        /// <summary>
        /// Stops an animated scroll so the player takes over from the current position.
        /// </summary>
        public void StopScrollAnimation() => _content.DOKill();

        private void Clear()
        {
            foreach (var item in _items)
            {
                if (item != null)
                    Destroy(item.gameObject);
            }

            _items.Clear();
        }

        private void OnDestroy() => _content.DOKill();
    }
}
