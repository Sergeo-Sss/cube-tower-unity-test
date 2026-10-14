using System;
using System.Threading;
using CubeTower.Core.Tower;
using CubeTower.Runtime.Config;
using CubeTower.Runtime.Gameplay.Cubes;
using CubeTower.Runtime.Gameplay.Effects;
using CubeTower.Runtime.Gameplay.Hole;
using CubeTower.Runtime.Gameplay.Tower;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;

namespace CubeTower.Runtime.Gameplay.Drag
{
    /// <summary>
    /// Owns the single active drag session and resolves the drop:
    /// hole - discard, tower - place / return, anywhere else - the cube is lost.
    /// </summary>
    public sealed class DragController : ITickable, IDisposable
    {
        // A drag is force-finished if the pointer has been released for this many frames
        // without OnEndDrag (focus loss, interrupted touches). Protects from a stuck session.
        private const int ReleasedFramesToCancel = 2;

        private readonly DragLayer _layer;
        private readonly TowerView _tower;
        private readonly HoleView _hole;
        private readonly CubeBurstEffect _burst;
        private readonly TowerGameplay _gameplay;
        private readonly CubeViewPool _pool;
        private readonly GameplaySettings _settings;
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();

        private Session _session;
        private int _releasedFrames;

        public DragController(DragLayer layer, TowerView tower, HoleView hole, CubeBurstEffect burst,
            TowerGameplay gameplay, CubeViewPool pool, GameplaySettings settings)
        {
            _layer = layer;
            _tower = tower;
            _hole = hole;
            _burst = burst;
            _gameplay = gameplay;
            _pool = pool;
            _settings = settings;
        }

        public bool IsDragging => _session != null;

        /// <summary>
        /// Starts dragging a new cube copied from the palette (the palette cube itself stays in place).
        /// </summary>
        public bool TryBeginFromPalette(CubeView source, PointerEventData eventData)
        {
            if (IsDragging || _lifetime.IsCancellationRequested)
                return false;

            var view = _pool.Spawn(source.Color);
            view.RectTransform.position = source.RectTransform.position;
            view.AttachTo(_layer.Root);
            Begin(new Session(DragOrigin.Palette, view, eventData.pointerId, 0), eventData);
            return true;
        }

        /// <summary>
        /// Starts dragging a cube that is already in the tower.
        /// </summary>
        public bool TryBeginFromTower(CubeView view, PointerEventData eventData)
        {
            if (IsDragging || _lifetime.IsCancellationRequested || !_tower.TryDetach(view, out var cubeId))
                return false;

            view.AttachTo(_layer.Root);
            Begin(new Session(DragOrigin.Tower, view, eventData.pointerId, cubeId), eventData);
            return true;
        }

        public void Drag(PointerEventData eventData)
        {
            if (Owns(eventData))
                MoveTo(eventData.position);
        }

        public void EndDrag(PointerEventData eventData)
        {
            if (!Owns(eventData))
                return;

            MoveTo(eventData.position);
            Finish();
        }

        void ITickable.Tick()
        {
            if (_session == null)
                return;

            var isPressed = Input.touchCount > 0 || Input.GetMouseButton(0);
            _releasedFrames = isPressed ? 0 : _releasedFrames + 1;
            if (_releasedFrames >= ReleasedFramesToCancel)
                Finish();
        }

        public void Dispose()
        {
            if (_lifetime.IsCancellationRequested)
                return;

            _lifetime.Cancel();
            _lifetime.Dispose();
            _session = null;
        }

        private void Begin(Session session, PointerEventData eventData)
        {
            _session = session;
            _releasedFrames = 0;

            var view = session.View;
            view.KillMotion();
            view.RectTransform.SetAsLastSibling();
            view.ScaleTo(_settings.Drag.PickupScale, _settings.Drag.PickupDuration, Ease.OutBack);

            // Keep the grab point under the finger instead of snapping the cube centre to it.
            session.GrabOffset = _layer.TryScreenToLocal(eventData.position, out var pointer)
                ? (Vector2)view.RectTransform.localPosition - pointer
                : Vector2.zero;
        }

        private bool Owns(PointerEventData eventData) => _session != null && _session.PointerId == eventData.pointerId;

        private void MoveTo(Vector2 screenPoint)
        {
            if (_layer.TryScreenToLocal(screenPoint, out var pointer))
                _session.View.RectTransform.localPosition = pointer + _session.GrabOffset;
        }

        private void Finish()
        {
            var session = _session;
            _session = null;
            Drop(session);
        }

        private void Drop(Session session)
        {
            var view = session.View;
            var screenCenter = _layer.WorldToScreen(view.RectTransform.position);

            if (_hole.Contains(screenCenter))
            {
                var discarded = session.Origin == DragOrigin.Tower
                    ? _gameplay.DiscardFromTower(session.TowerCubeId)
                    : DiscardNewCube();

                if (discarded)
                {
                    ReleaseAfter(_hole.PlayDiscardAsync(view, _lifetime.Token), view).Forget();
                    return;
                }
            }

            if (session.Origin == DragOrigin.Tower)
            {
                _tower.ReturnDetached(session.TowerCubeId, view);
                return;
            }

            DropResult result;
            using (_tower.AdoptIncoming(view))
                result = _gameplay.DropNewCube(view.Color, _tower.ScreenToAreaPoint(screenCenter), _tower.Area);

            if (!result.IsPlaced)
                ReleaseAfter(_burst.PlayAsync(view, _lifetime.Token), view).Forget();
        }

        private bool DiscardNewCube()
        {
            _gameplay.DiscardNewCube();
            return true;
        }

        private async UniTaskVoid ReleaseAfter(UniTask animation, CubeView view)
        {
            try
            {
                await animation;
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (!_lifetime.IsCancellationRequested && view != null)
                _pool.Despawn(view);
        }

        private enum DragOrigin
        {
            Palette,
            Tower
        }

        private sealed class Session
        {
            public Session(DragOrigin origin, CubeView view, int pointerId, int towerCubeId)
            {
                Origin = origin;
                View = view;
                PointerId = pointerId;
                TowerCubeId = towerCubeId;
            }

            public DragOrigin Origin { get; }
            public CubeView View { get; }
            public int PointerId { get; }
            public int TowerCubeId { get; }
            public Vector2 GrabOffset { get; set; }
        }
    }
}
