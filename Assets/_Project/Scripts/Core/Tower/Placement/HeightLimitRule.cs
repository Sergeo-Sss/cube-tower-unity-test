using CubeTower.Core.Messaging;

namespace CubeTower.Core.Tower.Placement
{
    /// <summary>
    /// The tower can't grow beyond the top of the tower area (top of the screen).
    /// </summary>
    public sealed class HeightLimitRule : IPlacementRule
    {
        public RuleVerdict Validate(in PlacementContext context)
        {
            var candidate = context.Candidate;
            var fits = candidate.Kind == PlacementKind.Base
                ? TowerLayout.Fits(candidate.BasePosition.y, 1, context.Area)
                : TowerLayout.Fits(context.Tower.BasePosition.y, context.Tower.Count + 1, context.Area);

            return fits ? RuleVerdict.Allow() : RuleVerdict.Deny(MessageKeys.HeightLimit);
        }
    }
}
