using UnityEngine;
using UnityEngine.UI;

namespace CubeTower.Runtime.UI
{
    /// <summary>
    /// Keeps the short side of the screen equal to the design's short side (1440 units) in both orientations.
    /// A plain "match height" scaler shrinks the canvas width to ~660 units in portrait and cuts the layout.
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    public sealed class ShortSideCanvasScaler : MonoBehaviour
    {
        [Tooltip("Design resolution in landscape orientation.")]
        [SerializeField] private Vector2 _landscapeReference = new Vector2(2560f, 1440f);

        private CanvasScaler _scaler;
        private Vector2Int _appliedScreen;

        private void Awake()
        {
            _scaler = GetComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            Apply(force: true);
        }

        private void Update() => Apply(force: false);

        private void Apply(bool force)
        {
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (!force && screen == _appliedScreen)
                return;

            _appliedScreen = screen;
            var isPortrait = screen.y > screen.x;
            _scaler.referenceResolution = isPortrait
                ? new Vector2(_landscapeReference.y, _landscapeReference.x)
                : _landscapeReference;
            // Match the short side: height in landscape, width in portrait.
            _scaler.matchWidthOrHeight = isPortrait ? 0f : 1f;
        }
    }
}
