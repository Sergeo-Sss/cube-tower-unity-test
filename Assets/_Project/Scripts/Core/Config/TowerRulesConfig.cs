using UnityEngine;

namespace CubeTower.Core.Config
{
    /// <summary>
    /// Tunable rules of the tower building.
    /// </summary>
    public sealed class TowerRulesConfig
    {
        public const float DefaultMaxHorizontalOffset = 0.5f;

        public TowerRulesConfig(float maxHorizontalOffset = DefaultMaxHorizontalOffset, bool requireSameColor = false)
        {
            MaxHorizontalOffset = float.IsNaN(maxHorizontalOffset) ? DefaultMaxHorizontalOffset : Mathf.Clamp01(maxHorizontalOffset);
            RequireSameColor = requireSameColor;
        }

        /// <summary>
        /// Max random horizontal shift of a cube relative to the cube below, in cube edge lengths (0.5 = half of the edge).
        /// </summary>
        public float MaxHorizontalOffset { get; }

        /// <summary>
        /// Example of a future rule: a cube can be stacked only onto a cube of the same colour.
        /// </summary>
        public bool RequireSameColor { get; }
    }
}
