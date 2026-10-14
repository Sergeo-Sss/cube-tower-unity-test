using UnityEngine;

namespace CubeTower.Runtime.Gameplay.Drag
{
    /// <summary>
    /// Topmost layer of the canvas where dragged cubes live, plus screen / canvas conversions.
    /// </summary>
    public sealed class DragLayer : MonoBehaviour
    {
        [SerializeField] private RectTransform _root;
        [SerializeField] private Canvas _canvas;

        public RectTransform Root => _root;

        /// <summary>
        /// Camera for screen conversions: <c>null</c> for Screen Space - Overlay canvases.
        /// </summary>
        public Camera EventCamera => _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;

        public bool TryScreenToLocal(Vector2 screenPoint, out Vector2 localPoint) =>
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screenPoint, EventCamera, out localPoint);

        public Vector2 WorldToScreen(Vector3 worldPosition) =>
            RectTransformUtility.WorldToScreenPoint(EventCamera, worldPosition);

        private void Reset()
        {
            _root = (RectTransform)transform;
            _canvas = GetComponentInParent<Canvas>();
        }
    }
}
