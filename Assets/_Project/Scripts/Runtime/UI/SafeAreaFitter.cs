using UnityEngine;

namespace CubeTower.Runtime.UI
{
    /// <summary>
    /// Fits the rect into <see cref="Screen.safeArea"/> (notches, rounded corners, home indicator).
    /// Edges can be excluded, e.g. the bottom panel background may go under the home indicator.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        [SerializeField] private bool _left = true;
        [SerializeField] private bool _right = true;
        [SerializeField] private bool _top = true;
        [SerializeField] private bool _bottom;

        private Rect _appliedSafeArea;
        private Vector2Int _appliedScreenSize;

        private void OnEnable() => Apply(force: true);

        private void Update() => Apply(force: false);

        private void Apply(bool force)
        {
            var safeArea = Screen.safeArea;
            var screenSize = new Vector2Int(Screen.width, Screen.height);
            if (screenSize.x <= 0 || screenSize.y <= 0)
                return;
            if (!force && safeArea == _appliedSafeArea && screenSize == _appliedScreenSize)
                return;

            _appliedSafeArea = safeArea;
            _appliedScreenSize = screenSize;

            var min = new Vector2(safeArea.xMin / screenSize.x, safeArea.yMin / screenSize.y);
            var max = new Vector2(safeArea.xMax / screenSize.x, safeArea.yMax / screenSize.y);
            if (!_left) min.x = 0f;
            if (!_bottom) min.y = 0f;
            if (!_right) max.x = 1f;
            if (!_top) max.y = 1f;

            var rectTransform = (RectTransform)transform;
            rectTransform.anchorMin = min;
            rectTransform.anchorMax = max;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
