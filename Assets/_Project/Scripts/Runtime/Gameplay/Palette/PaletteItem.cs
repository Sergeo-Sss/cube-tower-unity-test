using CubeTower.Core.Cubes;
using CubeTower.Runtime.Config;
using CubeTower.Runtime.Gameplay.Cubes;
using CubeTower.Runtime.Gameplay.Drag;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zenject;

namespace CubeTower.Runtime.Gameplay.Palette
{
    /// <summary>
    /// Infinite source of cubes in the bottom scroll.
    /// Resolves the scroll vs drag conflict by the direction of the gesture start: a steep gesture picks a cube,
    /// a flat one is forwarded to the <see cref="ScrollRect"/>. A gesture never does both.
    /// </summary>
    [RequireComponent(typeof(CubeView))]
    public sealed class PaletteItem : MonoBehaviour, ICubeDragReceiver
    {
        [SerializeField] private CubeView _cube;

        private DragController _drag;
        private CubeSkinCatalog _skins;
        private GameplaySettings _settings;
        private ScrollRect _scroll;
        private Gesture _gesture;

        public CubeColorId Color => _cube.Color;

        public CubeView Cube => _cube;

        [Inject]
        public void Construct(DragController drag, CubeSkinCatalog skins, GameplaySettings settings)
        {
            _drag = drag;
            _skins = skins;
            _settings = settings;
        }

        public void Setup(CubeColorId color, ScrollRect scroll)
        {
            _scroll = scroll;
            _cube.Setup(color, _skins.GetSprite(color));
            _cube.SetSize(_settings.CubeSize);
            _cube.SetDragReceiver(this);
        }

        public void OnInitializePotentialDrag(CubeView cube, PointerEventData eventData)
        {
            // Lets the scroll stop its inertia on touch, as it does for its own content.
            if (_scroll != null)
                _scroll.OnInitializePotentialDrag(eventData);
        }

        public void OnBeginDrag(CubeView cube, PointerEventData eventData)
        {
            if (IsPickGesture(eventData.position - eventData.pressPosition) && _drag.TryBeginFromPalette(_cube, eventData))
            {
                _gesture = Gesture.Cube;
                if (_scroll != null)
                    _scroll.StopMovement();
                return;
            }

            _gesture = Gesture.Scroll;
            if (_scroll != null)
                _scroll.OnBeginDrag(eventData);
        }

        public void OnDrag(CubeView cube, PointerEventData eventData)
        {
            switch (_gesture)
            {
                case Gesture.Cube:
                    _drag.Drag(eventData);
                    break;
                case Gesture.Scroll when _scroll != null:
                    _scroll.OnDrag(eventData);
                    break;
            }
        }

        public void OnEndDrag(CubeView cube, PointerEventData eventData)
        {
            switch (_gesture)
            {
                case Gesture.Cube:
                    _drag.EndDrag(eventData);
                    break;
                case Gesture.Scroll when _scroll != null:
                    _scroll.OnEndDrag(eventData);
                    break;
            }

            _gesture = Gesture.None;
        }

        private bool IsPickGesture(Vector2 delta)
        {
            if (delta.sqrMagnitude <= Mathf.Epsilon)
                return false;

            var angle = Mathf.Atan2(Mathf.Abs(delta.y), Mathf.Abs(delta.x)) * Mathf.Rad2Deg;
            return angle >= _settings.Drag.PickAngle;
        }

        private void Reset() => _cube = GetComponent<CubeView>();

        private enum Gesture
        {
            None,
            Scroll,
            Cube
        }

        public sealed class Factory : PlaceholderFactory<PaletteItem>
        {
        }
    }
}
