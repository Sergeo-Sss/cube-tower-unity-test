using System;
using System.Collections.Generic;
using CubeTower.Core.Cubes;
using CubeTower.Core.Randomness;
using UnityEngine;

namespace CubeTower.Core.Tower.Placement
{
    /// <summary>
    /// Decides where a dropped cube goes. Pure logic: works with area coordinates provided by the view.
    /// </summary>
    public sealed class TowerPlacementService
    {
        private readonly TowerModel _tower;
        private readonly IReadOnlyList<IPlacementRule> _rules;
        private readonly IRandomSource _random;
        private readonly List<Vector2> _centers = new List<Vector2>();

        public TowerPlacementService(TowerModel tower, List<IPlacementRule> rules, IRandomSource random)
        {
            _tower = tower ?? throw new ArgumentNullException(nameof(tower));
            _rules = rules ?? (IReadOnlyList<IPlacementRule>)Array.Empty<IPlacementRule>();
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <param name="color">Colour of the dropped cube.</param>
        /// <param name="cubeCenter">Centre of the dropped cube in area coordinates.</param>
        /// <param name="area">Current geometry of the tower area.</param>
        public PlacementResult Evaluate(CubeColorId color, Vector2 cubeCenter, TowerArea area)
        {
            if (!area.IsValid || !color.IsValid)
                return PlacementResult.Missed();

            PlacementCandidate candidate;
            if (_tower.IsEmpty)
            {
                if (!area.Bounds.Contains(cubeCenter))
                    return PlacementResult.Missed();

                candidate = PlacementCandidate.Base(color, TowerLayout.ToBasePosition(cubeCenter, area));
            }
            else
            {
                TowerLayout.CalculateCenters(_tower, area, _centers);
                if (!OverlapsTower(cubeCenter, area.CubeSize))
                    return PlacementResult.Missed();

                candidate = PlacementCandidate.Stack(color, PickOffset(area));
            }

            var context = new PlacementContext(_tower, area, candidate);
            for (var i = 0; i < _rules.Count; i++)
            {
                var verdict = _rules[i].Validate(context);
                if (!verdict.IsAllowed)
                    return PlacementResult.Rejected(candidate, verdict.ReasonKey);
            }

            return PlacementResult.Accepted(candidate);
        }

        public TowerCube Apply(in PlacementCandidate candidate) => candidate.Kind == PlacementKind.Base
            ? _tower.PlaceBase(candidate.Color, candidate.BasePosition)
            : _tower.Stack(candidate.Color, candidate.Offset);

        private bool OverlapsTower(Vector2 cubeCenter, float cubeSize)
        {
            var dropped = TowerLayout.CubeRect(cubeCenter, cubeSize);
            for (var i = 0; i < _centers.Count; i++)
            {
                if (dropped.Overlaps(TowerLayout.CubeRect(_centers[i], cubeSize)))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Random offset within the max offset rule that also keeps the new cube inside the area horizontally.
        /// </summary>
        private float PickOffset(TowerArea area)
        {
            var size = area.CubeSize;
            var half = size * 0.5f;
            var topX = _centers[_centers.Count - 1].x;

            var min = Mathf.Max(-_tower.MaxOffset, (half - topX) / size);
            var max = Mathf.Min(_tower.MaxOffset, (area.Size.x - half - topX) / size);
            if (min > max)
                return 0f;

            return _random.Range(min, max);
        }
    }
}
