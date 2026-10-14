using CubeTower.Core.Config;
using CubeTower.Core.Cubes;
using CubeTower.Core.Randomness;

namespace CubeTower.Tests
{
    internal static class Colors
    {
        public static readonly CubeColorId Red = new CubeColorId("red");
        public static readonly CubeColorId Green = new CubeColorId("green");
        public static readonly CubeColorId Blue = new CubeColorId("blue");
    }

    internal static class TestConfigs
    {
        public static GameConfig Default(bool requireSameColor = false) =>
            GameConfig.Create(3, new[] { Colors.Red, Colors.Green, Colors.Blue }, new TowerRulesConfig(0.5f, requireSameColor));
    }

    /// <summary>
    /// Returns a fixed fraction of the requested range: 0 - min, 1 - max.
    /// </summary>
    internal sealed class FixedRandom : IRandomSource
    {
        private readonly float _t;

        public FixedRandom(float t)
        {
            _t = t;
        }

        public float Range(float min, float max) => min + (max - min) * _t;
    }
}
