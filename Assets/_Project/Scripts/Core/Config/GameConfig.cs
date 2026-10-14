using System;
using System.Collections.Generic;
using System.Linq;
using CubeTower.Core.Cubes;

namespace CubeTower.Core.Config
{
    /// <summary>
    /// Source-agnostic game configuration. Built by an <see cref="IGameConfigProvider"/> implementation.
    /// </summary>
    public sealed class GameConfig
    {
        private readonly HashSet<CubeColorId> _colors;

        public GameConfig(IReadOnlyList<CubeColorId> paletteCubes, TowerRulesConfig towerRules)
        {
            if (paletteCubes == null)
                throw new ArgumentNullException(nameof(paletteCubes));
            if (paletteCubes.Count == 0)
                throw new GameConfigException("Palette must contain at least one cube.");
            if (paletteCubes.Any(id => !id.IsValid))
                throw new GameConfigException("Palette contains a cube without colour id.");

            PaletteCubes = paletteCubes.ToArray();
            TowerRules = towerRules ?? throw new ArgumentNullException(nameof(towerRules));
            _colors = new HashSet<CubeColorId>(PaletteCubes);
        }

        /// <summary>
        /// Cubes of the bottom scroll, in display order.
        /// </summary>
        public IReadOnlyList<CubeColorId> PaletteCubes { get; }

        public IReadOnlyCollection<CubeColorId> Colors => _colors;

        public TowerRulesConfig TowerRules { get; }

        public bool HasColor(CubeColorId id) => _colors.Contains(id);

        /// <summary>
        /// Builds a palette of <paramref name="cubesCount"/> cubes cycling through <paramref name="colors"/>.
        /// </summary>
        public static GameConfig Create(int cubesCount, IReadOnlyList<CubeColorId> colors, TowerRulesConfig towerRules)
        {
            if (cubesCount <= 0)
                throw new GameConfigException($"Cubes count must be positive, got {cubesCount}.");
            if (colors == null || colors.Count == 0)
                throw new GameConfigException("At least one cube colour must be configured.");

            var palette = new CubeColorId[cubesCount];
            for (var i = 0; i < cubesCount; i++)
                palette[i] = colors[i % colors.Count];

            return new GameConfig(palette, towerRules);
        }
    }

    public sealed class GameConfigException : Exception
    {
        public GameConfigException(string message) : base(message)
        {
        }
    }
}
