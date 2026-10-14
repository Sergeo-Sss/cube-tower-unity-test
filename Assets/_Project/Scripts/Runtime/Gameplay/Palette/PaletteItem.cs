using CubeTower.Core.Cubes;
using CubeTower.Core.Gestures;
using CubeTower.Runtime.Config;
using CubeTower.Runtime.Gameplay.Cubes;
using CubeTower.Runtime.Gameplay.Drag;
using CubeTower.Runtime.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zenject;

namespace CubeTower.Runtime.Gameplay.Palette
{
    /// <summary>
    /// Infinite source of cubes in the bottom scroll.
    /// Resolves the scroll vs drag conflict with <see cref="DragGestureClassifier"/>: the intent is decided
    /// once (by hold time or by direction over a meaningful distance) and a gesture never does both.
    /// Until the intent is clear nothing moves - neither the scroll nor a cube.
    /// </summary>
    [RequireComponent(typeof(CubeView))]
    public sealed class PaletteItem : MonoBehaviour, ICubeDragReceiver
    {
        [SerializeField] private CubeView _cube;

        private DragController _drag;
        private CubeSkinCatalog _skins;
        private GameplaySettings _settings;
        private DragGestureClassifier _classifier;
        private ScrollRect _scroll;
        private Gesture _gesture;
        private float _pressTime;
        private float _heldBeforeMove;

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
            _pressTime = Time.unscaledTime;

            // Lets the scroll stop its inertia on touch, as it does for its own content.
            if (_scroll != null)
                _scroll.OnInitializePotentialDrag(eventData);
        }

        public void OnBeginDrag(CubeView cube, PointerEventData eventData)
        {
            _heldBeforeMove = Time.unscaledTime - _pressTime;
            _gesture = Gesture.Undecided;
            TryDecide(eventData);
        }

        public void OnDrag(CubeView cube, PointerEventData eventData)
        {
            if (_gesture == Gesture.Undecided)
                TryDecide(eventData);

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

        private void TryDecide(PointerEventData eventData)
        {
            var intent = Classifier.Classify(eventData.position - eventData.pressPosition, _heldBeforeMove);
            if (intent == DragGesture.Undecided)
                return;

            if (intent == DragGesture.PickCube && _drag.TryBeginFromPalette(_cube, eventData))
            {
                _gesture = Gesture.Cube;
                if (_scroll != null)
                    _scroll.StopMovement();
                return;
            }

            // The scroll starts from the current pointer position, so the content doesn't jump.
            _gesture = Gesture.Scroll;
            if (_scroll != null)
                _scroll.OnBeginDrag(eventData);
        }

        // Created lazily: DPI is only reliable at runtime.
        private DragGestureClassifier Classifier => _classifier ??= new DragGestureClassifier(
            _settings.Drag.PickAngle,
            ScreenDpi.InchesToPixels(_settings.Drag.DecisionDistanceInches),
            _settings.Drag.HoldToPick);

        private void Reset() => _cube = GetComponent<CubeView>();

        private enum Gesture
        {
            None,
            Undecided,
            Scroll,
            Cube
        }

        public sealed class Factory : PlaceholderFactory<PaletteItem>
        {
        }
    }
}
