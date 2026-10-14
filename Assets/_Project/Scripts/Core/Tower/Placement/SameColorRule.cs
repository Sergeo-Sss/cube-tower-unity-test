using CubeTower.Core.Config;
using CubeTower.Core.Messaging;

namespace CubeTower.Core.Tower.Placement
{
    /// <summary>
    /// Optional rule: a cube can be stacked only onto a cube of the same colour.
    /// Enabled from the config (<see cref="TowerRulesConfig.RequireSameColor"/>).
    /// </summary>
    public sealed class SameColorRule : IPlacementRule
    {
        private readonly bool _enabled;

        public SameColorRule(TowerRulesConfig rules)
        {
            _enabled = rules.RequireSameColor;
        }

        public RuleVerdict Validate(in PlacementContext context)
        {
            if (!_enabled || context.Candidate.Kind != PlacementKind.Stack || context.Tower.IsEmpty)
                return RuleVerdict.Allow();

            return context.Tower.Top.Color == context.Candidate.Color
                ? RuleVerdict.Allow()
                : RuleVerdict.Deny(MessageKeys.ColorMismatch);
        }
    }
}
