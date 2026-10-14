using CubeTower.Core.Cubes;

namespace CubeTower.Core.Tower
{
    /// <summary>
    /// A cube placed into the tower.
    /// </summary>
    public sealed class TowerCube
    {
        internal TowerCube(int id, CubeColorId color, float offset)
        {
            Id = id;
            Color = color;
            Offset = offset;
        }

        /// <summary>
        /// Unique (per session) identifier, stable while the cube stays in the tower.
        /// </summary>
        public int Id { get; }

        public CubeColorId Color { get; }

        /// <summary>
        /// Horizontal shift relative to the cube below, in cube edge lengths. Always 0 for the base cube.
        /// </summary>
        public float Offset { get; internal set; }

        public override string ToString() => $"#{Id} {Color} ({Offset:+0.00;-0.00})";
    }
}
