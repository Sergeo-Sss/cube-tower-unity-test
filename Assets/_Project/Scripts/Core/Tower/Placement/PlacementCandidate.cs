using CubeTower.Core.Cubes;
using UnityEngine;

namespace CubeTower.Core.Tower.Placement
{
    public enum PlacementKind
    {
        /// <summary>First cube of the tower, placed anywhere inside the area.</summary>
        Base,

        /// <summary>Cube stacked on top of the tower.</summary>
        Stack
    }

    /// <summary>
    /// A not yet applied placement of a cube into the tower.
    /// </summary>
    public readonly struct PlacementCandidate
    {
        private PlacementCandidate(PlacementKind kind, CubeColorId color, Vector2 basePosition, float offset)
        {
            Kind = kind;
            Color = color;
            BasePosition = basePosition;
            Offset = offset;
        }

        public PlacementKind Kind { get; }

        public CubeColorId Color { get; }

        /// <summary>Model position of the base cube (only for <see cref="PlacementKind.Base"/>).</summary>
        public Vector2 BasePosition { get; }

        /// <summary>Horizontal offset relative to the current top cube (only for <see cref="PlacementKind.Stack"/>).</summary>
        public float Offset { get; }

        public static PlacementCandidate Base(CubeColorId color, Vector2 basePosition) =>
            new PlacementCandidate(PlacementKind.Base, color, basePosition, 0f);

        public static PlacementCandidate Stack(CubeColorId color, float offset) =>
            new PlacementCandidate(PlacementKind.Stack, color, Vector2.zero, offset);
    }
}
