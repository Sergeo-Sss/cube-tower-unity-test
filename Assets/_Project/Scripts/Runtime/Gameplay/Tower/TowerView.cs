using System;
using System.Collections.Generic;
using CubeTower.Core.Tower;
using CubeTower.Runtime.Config;
using CubeTower.Runtime.Gameplay.Cubes;
using CubeTower.Runtime.Gameplay.Drag;
using DG.Tweening;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;

namespace CubeTower.Runtime.Gameplay.Tower
{
    /// <summary>
    /// Visual of <see cref="TowerModel"/>. Reacts to model changes and animates cubes into their layout slots.
    /// The component sits on the tower area: its rect defines where the tower can be built.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class TowerView : MonoBehaviour, ICubeDragReceiver
    {
        private const float SameTargetSqrTolerance = 0.01f;

        private readonly Dictionary<int, CubeView> _views = new Dictionary<int, CubeView>();
        private readonly Dictionary<CubeView, int> _ids = new Dictionary<CubeView, int>();
        private readonly Dictionary<int, Vector2> _targets = new Dictionary<int, Vector2>();
        private readonly HashSet<int> _detached = new HashSet<int>();
        private readonly List<Vector2> _centers = new List<Vector2>();

        private RectTransform _area;
        private TowerModel _tower;
        private CubeViewPool _pool;
        private GameplaySettings _settings;
        private LazyInject<DragController> _drag;
        private DragLayer _layer;
        private IDisposable _subscription;
        private CubeView _incoming;
        private bool _initialized;

        public TowerArea Area => new TowerArea(AreaRect.rect.size, _settings.CubeSize);

        private RectTransform AreaRect => _area != null ? _area : _area = (RectTransform)transform;

        [Inject]
        public void Construct(TowerModel tower, CubeViewPool pool, GameplaySettings settings,
            LazyInject<DragController> drag, DragLayer layer)
        {
            _tower = tower;
            _pool = pool;
            _settings = settings;
            _drag = drag;
            _layer = layer;
        }

        public void Initialize()
        {
            if (_initialized)
                return;

            _subscription = _tower.Changes.Subscribe(OnTowerChanged);
            _initialized = true;
            Rebuild();
        }

        public Vector2 ScreenToAreaPoint(Vector2 screenPoint)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(AreaRect, screenPoint, _layer.EventCamera, out var local);
            return local - AreaRect.rect.min;
        }

        public Vector3 AreaToWorld(Vector2 areaPoint) => AreaRect.TransformPoint(areaPoint + AreaRect.rect.min);

        /// <summary>
        /// Visual of the top cube if it is currently in the tower (not dragged).
        /// </summary>
        public bool TryGetTopCube(out CubeView view)
        {
            view = null;
            var top = _tower.Top;
            return top != null && !_detached.Contains(top.Id) && _views.TryGetValue(top.Id, out view);
        }

        /// <summary>
        /// Within the returned scope the next cube added to the model reuses <paramref name="view"/>
        /// (the cube the player is dropping) instead of spawning a new one.
        /// </summary>
        public IDisposable AdoptIncoming(CubeView view)
        {
            _incoming = view;
            return Disposable.Create(() => _incoming = null);
        }

        /// <summary>
        /// Hands a tower cube over to the drag. The cube stays in the model until the drop is resolved.
        /// </summary>
        public bool TryDetach(CubeView view, out int cubeId)
        {
            if (!_ids.TryGetValue(view, out cubeId) || !_detached.Add(cubeId))
                return false;

            _targets.Remove(cubeId);
            return true;
        }

        /// <summary>
        /// Puts a detached cube back to its slot.
        /// </summary>
        public void ReturnDetached(int cubeId, CubeView view)
        {
            _detached.Remove(cubeId);
            if (!_views.TryGetValue(cubeId, out var registered) || registered != view)
            {
                _pool.Despawn(view);
                return;
            }

            view.AttachToArea(AreaRect);
            view.ScaleTo(1f, _settings.Animation.ReturnDuration);
            Layout(animate: true, returningCubeId: cubeId);
        }

        void ICubeDragReceiver.OnInitializePotentialDrag(CubeView cube, PointerEventData eventData)
        {
        }

        void ICubeDragReceiver.OnBeginDrag(CubeView cube, PointerEventData eventData) => _drag.Value.TryBeginFromTower(cube, eventData);

        void ICubeDragReceiver.OnDrag(CubeView cube, PointerEventData eventData) => _drag.Value.Drag(eventData);

        void ICubeDragReceiver.OnEndDrag(CubeView cube, PointerEventData eventData) => _drag.Value.EndDrag(eventData);

        private void OnTowerChanged(TowerChange change)
        {
            switch (change.Type)
            {
                case TowerChangeType.Added:
                    OnCubeAdded(change.Cube);
                    break;
                case TowerChangeType.Removed:
                    OnCubeRemoved(change.Cube);
                    break;
                case TowerChangeType.Reset:
                    Rebuild();
                    break;
            }
        }

        private void OnCubeAdded(TowerCube cube)
        {
            var view = _incoming != null ? _incoming : _pool.Spawn(cube.Color);
            _incoming = null;

            Register(cube.Id, view);
            view.ScaleTo(1f, _settings.Animation.JumpDuration);
            Layout(animate: true, jumpingCubeId: cube.Id);
        }

        private void OnCubeRemoved(TowerCube cube)
        {
            if (_views.TryGetValue(cube.Id, out var view))
            {
                _views.Remove(cube.Id);
                _ids.Remove(view);
                _targets.Remove(cube.Id);

                // A detached cube is owned by the drag (e.g. it is falling into the hole right now).
                if (!_detached.Remove(cube.Id))
                    _pool.Despawn(view);
            }

            Layout(animate: true);
        }

        private void Rebuild()
        {
            foreach (var pair in _views)
            {
                if (!_detached.Contains(pair.Key))
                    _pool.Despawn(pair.Value);
            }

            _views.Clear();
            _ids.Clear();
            _targets.Clear();
            _detached.Clear();

            foreach (var cube in _tower.Cubes)
                Register(cube.Id, _pool.Spawn(cube.Color));

            Layout(animate: false);
        }

        private void Register(int cubeId, CubeView view)
        {
            _views[cubeId] = view;
            _ids[view] = cubeId;
            view.AttachToArea(AreaRect);
            view.SetDragReceiver(this);
        }

        private void Layout(bool animate, int jumpingCubeId = 0, int returningCubeId = 0)
        {
            TowerLayout.CalculateCenters(_tower, Area, _centers);

            var animation = _settings.Animation;
            var cubes = _tower.Cubes;
            for (var i = 0; i < cubes.Count && i < _centers.Count; i++)
            {
                var id = cubes[i].Id;
                if (_detached.Contains(id) || !_views.TryGetValue(id, out var view))
                    continue;

                var target = _centers[i];
                if (_targets.TryGetValue(id, out var current) && (current - target).sqrMagnitude < SameTargetSqrTolerance)
                    continue;

                _targets[id] = target;
                if (!animate)
                    view.SnapTo(target);
                else if (id == jumpingCubeId)
                    view.JumpTo(target, animation.JumpPower, animation.JumpDuration);
                else if (id == returningCubeId)
                    view.MoveTo(target, animation.ReturnDuration, Ease.OutBack);
                else
                    view.MoveTo(target, animation.CollapseDuration, animation.CollapseEase);
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!_initialized || !isActiveAndEnabled)
                return;

            _targets.Clear();
            Layout(animate: false);
        }

        private void OnDestroy()
        {
            _subscription?.Dispose();
            _initialized = false;
        }
    }
}
