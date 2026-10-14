using System;
using System.Collections.Generic;
using CubeTower.Core.Config;
using CubeTower.Core.Cubes;
using UniRx;
using UnityEngine;

namespace CubeTower.Core.Tower
{
    /// <summary>
    /// Resolution independent state of the tower.
    /// All coordinates are expressed in cube edge lengths, so the same state can be laid out
    /// on any screen / aspect ratio (see <see cref="TowerLayout"/>).
    /// </summary>
    public sealed class TowerModel : IDisposable
    {
        private readonly List<TowerCube> _cubes = new List<TowerCube>();
        private readonly Subject<TowerChange> _changes = new Subject<TowerChange>();
        private readonly float _maxOffset;
        private int _nextId = 1;

        public TowerModel(TowerRulesConfig rules)
        {
            if (rules == null)
                throw new ArgumentNullException(nameof(rules));

            _maxOffset = rules.MaxHorizontalOffset;
        }

        public IReadOnlyList<TowerCube> Cubes => _cubes;

        public int Count => _cubes.Count;

        public bool IsEmpty => _cubes.Count == 0;

        public TowerCube Top => IsEmpty ? null : _cubes[_cubes.Count - 1];

        /// <summary>
        /// Position of the base cube in cube units: X - centre of the cube relative to the horizontal
        /// centre of the tower area, Y - bottom edge of the cube relative to the bottom of the area.
        /// </summary>
        public Vector2 BasePosition { get; private set; }

        public float MaxOffset => _maxOffset;

        public IObservable<TowerChange> Changes => _changes;

        public TowerCube PlaceBase(CubeColorId color, Vector2 basePosition)
        {
            if (!IsEmpty)
                throw new InvalidOperationException("Base cube can only be placed into an empty tower.");

            BasePosition = Sanitize(basePosition);
            return Add(color, 0f);
        }

        public TowerCube Stack(CubeColorId color, float offset)
        {
            if (IsEmpty)
                throw new InvalidOperationException("Cannot stack a cube onto an empty tower, place the base first.");

            return Add(color, ClampOffset(offset));
        }

        public int IndexOf(int cubeId)
        {
            for (var i = 0; i < _cubes.Count; i++)
            {
                if (_cubes[i].Id == cubeId)
                    return i;
            }

            return -1;
        }

        public bool Contains(int cubeId) => IndexOf(cubeId) >= 0;

        /// <summary>
        /// Removes a cube from any position. Cubes above fall down by one cell and keep
        /// their horizontal positions as much as the max offset rule allows.
        /// </summary>
        public bool TryRemove(int cubeId, out TowerCube removed)
        {
            var index = IndexOf(cubeId);
            if (index < 0)
            {
                removed = null;
                return false;
            }

            removed = _cubes[index];
            _cubes.RemoveAt(index);

            if (index < _cubes.Count)
            {
                var next = _cubes[index];
                if (index == 0)
                {
                    // The cube above becomes the new base: keep it where it was horizontally.
                    BasePosition = new Vector2(BasePosition.x + next.Offset, BasePosition.y);
                    next.Offset = 0f;
                }
                else
                {
                    next.Offset = ClampOffset(next.Offset + removed.Offset);
                }
            }

            _changes.OnNext(TowerChange.Removed(removed, index));
            return true;
        }

        /// <summary>
        /// Replaces the whole state (used to restore saved progress).
        /// </summary>
        public void Restore(Vector2 basePosition, IReadOnlyList<(CubeColorId Color, float Offset)> cubes)
        {
            if (cubes == null)
                throw new ArgumentNullException(nameof(cubes));

            _cubes.Clear();
            BasePosition = Sanitize(basePosition);
            for (var i = 0; i < cubes.Count; i++)
            {
                var offset = i == 0 ? 0f : ClampOffset(cubes[i].Offset);
                _cubes.Add(new TowerCube(_nextId++, cubes[i].Color, offset));
            }

            _changes.OnNext(TowerChange.Reset());
        }

        public void Clear()
        {
            _cubes.Clear();
            BasePosition = Vector2.zero;
            _changes.OnNext(TowerChange.Reset());
        }

        public void Dispose()
        {
            _changes.OnCompleted();
            _changes.Dispose();
        }

        private TowerCube Add(CubeColorId color, float offset)
        {
            if (!color.IsValid)
                throw new ArgumentException("Cube colour id is not valid.", nameof(color));

            var cube = new TowerCube(_nextId++, color, offset);
            _cubes.Add(cube);
            _changes.OnNext(TowerChange.Added(cube, _cubes.Count - 1));
            return cube;
        }

        private float ClampOffset(float offset) => float.IsNaN(offset) || float.IsInfinity(offset)
            ? 0f
            : Mathf.Clamp(offset, -_maxOffset, _maxOffset);

        private static Vector2 Sanitize(Vector2 position)
        {
            var x = float.IsNaN(position.x) || float.IsInfinity(position.x) ? 0f : position.x;
            var y = float.IsNaN(position.y) || float.IsInfinity(position.y) ? 0f : Mathf.Max(0f, position.y);
            return new Vector2(x, y);
        }
    }
}
