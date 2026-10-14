namespace CubeTower.Core.Tower
{
    public enum TowerChangeType
    {
        Added,
        Removed,
        Reset
    }

    /// <summary>
    /// Change notification of <see cref="TowerModel"/>.
    /// </summary>
    public readonly struct TowerChange
    {
        private TowerChange(TowerChangeType type, TowerCube cube, int index)
        {
            Type = type;
            Cube = cube;
            Index = index;
        }

        public TowerChangeType Type { get; }

        /// <summary>
        /// Affected cube. <c>null</c> for <see cref="TowerChangeType.Reset"/>.
        /// </summary>
        public TowerCube Cube { get; }

        /// <summary>
        /// Index of the cube in the tower at the moment of the change (-1 for reset).
        /// </summary>
        public int Index { get; }

        public static TowerChange Added(TowerCube cube, int index) => new TowerChange(TowerChangeType.Added, cube, index);

        public static TowerChange Removed(TowerCube cube, int index) => new TowerChange(TowerChangeType.Removed, cube, index);

        public static TowerChange Reset() => new TowerChange(TowerChangeType.Reset, null, -1);
    }
}
