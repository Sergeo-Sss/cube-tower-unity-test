using System;
using CubeTower.Core.Cubes;
using CubeTower.Core.Messaging;
using CubeTower.Core.Tower.Placement;
using UnityEngine;

namespace CubeTower.Core.Tower
{
    /// <summary>
    /// Gameplay use cases of the tower. Single place that changes the model and reports
    /// user facing feedback, so views stay dumb and rules stay testable.
    /// </summary>
    public sealed class TowerGameplay
    {
        private readonly TowerModel _tower;
        private readonly TowerPlacementService _placement;
        private readonly IGameMessenger _messenger;

        public TowerGameplay(TowerModel tower, TowerPlacementService placement, IGameMessenger messenger)
        {
            _tower = tower ?? throw new ArgumentNullException(nameof(tower));
            _placement = placement ?? throw new ArgumentNullException(nameof(placement));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        }

        /// <summary>
        /// A new cube from the palette was dropped at <paramref name="cubeCenter"/> (tower area coordinates).
        /// </summary>
        public DropResult DropNewCube(CubeColorId color, Vector2 cubeCenter, TowerArea area)
        {
            var result = _placement.Evaluate(color, cubeCenter, area);
            switch (result.Status)
            {
                case PlacementStatus.Accepted:
                    var cube = _placement.Apply(result.Candidate);
                    _messenger.Publish(MessageKeys.CubePlaced);
                    return DropResult.Placed(cube);

                case PlacementStatus.Rejected:
                    _messenger.Publish(result.ReasonKey ?? MessageKeys.CubeMissed);
                    return DropResult.Lost();

                default:
                    _messenger.Publish(MessageKeys.CubeMissed);
                    return DropResult.Lost();
            }
        }

        /// <summary>
        /// A cube of the tower was thrown into the hole.
        /// </summary>
        public bool DiscardFromTower(int cubeId)
        {
            if (!_tower.TryRemove(cubeId, out _))
                return false;

            _messenger.Publish(MessageKeys.CubeDiscarded);
            return true;
        }

        /// <summary>
        /// A new cube from the palette was thrown straight into the hole.
        /// </summary>
        public void DiscardNewCube() => _messenger.Publish(MessageKeys.CubeDiscarded);
    }

    public readonly struct DropResult
    {
        private DropResult(TowerCube cube)
        {
            Cube = cube;
        }

        /// <summary>Placed cube, <c>null</c> if the cube is lost.</summary>
        public TowerCube Cube { get; }

        public bool IsPlaced => Cube != null;

        public static DropResult Placed(TowerCube cube) => new DropResult(cube);

        public static DropResult Lost() => new DropResult(null);
    }
}
