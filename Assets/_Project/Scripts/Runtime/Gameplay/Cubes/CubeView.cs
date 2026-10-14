using CubeTower.Core.Cubes;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CubeTower.Runtime.Gameplay.Cubes
{
    /// <summary>
    /// Visual of a single cube. Forwards drag gestures to the current <see cref="ICubeDragReceiver"/>
    /// and provides motion helpers that never leave two competing tweens on the same cube.
    /// </summary>
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public sealed class CubeView : MonoBehaviour,
        IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Image _image;
        [SerializeField] private CanvasGroup _canvasGroup;

        private RectTransform _rectTransform;
        private ICubeDragReceiver _dragReceiver;
        private Tween _motion;
        private Tween _scale;

        public RectTransform RectTransform => _rectTransform != null ? _rectTransform : _rectTransform = (RectTransform)transform;

        public CanvasGroup CanvasGroup => _canvasGroup;

        public Sprite Sprite => _image.sprite;

        public CubeColorId Color { get; private set; }

        public void Setup(CubeColorId color, Sprite sprite)
        {
            Color = color;
            _image.sprite = sprite;
            ResetVisualState();
        }

        /// <summary>
        /// Enables gestures on the cube; <c>null</c> makes the cube transparent for input.
        /// </summary>
        public void SetDragReceiver(ICubeDragReceiver receiver)
        {
            _dragReceiver = receiver;
            _image.raycastTarget = receiver != null;
        }

        public void SetSize(float size) => RectTransform.sizeDelta = new Vector2(size, size);

        /// <summary>
        /// Extends (negative values) or shrinks the touch area around the visible cube: left, bottom, right, top.
        /// </summary>
        public void SetTouchPadding(Vector4 padding) => _image.raycastPadding = padding;

        /// <summary>
        /// Re-parents the cube keeping its on-screen position and anchors it to the bottom-left corner of the parent,
        /// so <see cref="RectTransform.anchoredPosition"/> equals area coordinates.
        /// </summary>
        public void AttachToArea(RectTransform parent)
        {
            var rectTransform = RectTransform;
            var worldPosition = rectTransform.position;
            rectTransform.SetParent(parent, false);
            rectTransform.anchorMin = rectTransform.anchorMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.position = worldPosition;
        }

        /// <summary>
        /// Re-parents the cube keeping its on-screen position, anchored to the parent's centre.
        /// </summary>
        public void AttachTo(RectTransform parent)
        {
            var rectTransform = RectTransform;
            var worldPosition = rectTransform.position;
            rectTransform.SetParent(parent, false);
            rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.position = worldPosition;
        }

        public void SnapTo(Vector2 anchoredPosition)
        {
            KillMotion();
            RectTransform.anchoredPosition = anchoredPosition;
        }

        public Tween MoveTo(Vector2 anchoredPosition, float duration, Ease ease)
        {
            KillMotion();
            return _motion = RectTransform.DOAnchorPos(anchoredPosition, duration).SetEase(ease).SetLink(gameObject);
        }

        public Tween JumpTo(Vector2 anchoredPosition, float power, float duration)
        {
            KillMotion();
            return _motion = RectTransform.DOJumpAnchorPos(anchoredPosition, power, 1, duration)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject);
        }

        public Tween ScaleTo(float scale, float duration, Ease ease = Ease.OutQuad)
        {
            _scale?.Kill();
            return _scale = RectTransform.DOScale(scale, duration).SetEase(ease).SetLink(gameObject);
        }

        public void KillMotion()
        {
            _motion?.Kill();
            _motion = null;
        }

        /// <summary>
        /// Stops every animation of the cube keeping its current transform.
        /// </summary>
        public void KillAnimations()
        {
            KillMotion();
            _scale = null;
            RectTransform.DOKill();
            _canvasGroup.DOKill();
        }

        /// <summary>
        /// Stops all animations and restores default transform / opacity. Called when the cube is (re)used.
        /// </summary>
        public void ResetVisualState()
        {
            KillAnimations();
            RectTransform.localScale = Vector3.one;
            RectTransform.localRotation = Quaternion.identity;
            _canvasGroup.alpha = 1f;
        }

        void IInitializePotentialDragHandler.OnInitializePotentialDrag(PointerEventData eventData) =>
            _dragReceiver?.OnInitializePotentialDrag(this, eventData);

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData) => _dragReceiver?.OnBeginDrag(this, eventData);

        void IDragHandler.OnDrag(PointerEventData eventData) => _dragReceiver?.OnDrag(this, eventData);

        void IEndDragHandler.OnEndDrag(PointerEventData eventData) => _dragReceiver?.OnEndDrag(this, eventData);

        private void OnDestroy()
        {
            KillMotion();
            RectTransform.DOKill();
            if (_canvasGroup != null)
                _canvasGroup.DOKill();
        }

        private void Reset()
        {
            _image = GetComponent<Image>();
            _canvasGroup = GetComponent<CanvasGroup>();
        }
    }
}
