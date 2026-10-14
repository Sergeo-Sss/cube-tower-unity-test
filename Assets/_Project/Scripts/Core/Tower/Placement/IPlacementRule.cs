namespace CubeTower.Core.Tower.Placement
{
    /// <summary>
    /// Extension point of the tower building. Every registered rule must allow a placement.
    /// New gameplay restrictions are added as new rules without changing existing code.
    /// </summary>
    public interface IPlacementRule
    {
        RuleVerdict Validate(in PlacementContext context);
    }

    public readonly struct PlacementContext
    {
        public PlacementContext(TowerModel tower, TowerArea area, PlacementCandidate candidate)
        {
            Tower = tower;
            Area = area;
            Candidate = candidate;
        }

        public TowerModel Tower { get; }

        public TowerArea Area { get; }

        public PlacementCandidate Candidate { get; }
    }

    public readonly struct RuleVerdict
    {
        private RuleVerdict(bool isAllowed, string reasonKey)
        {
            IsAllowed = isAllowed;
            ReasonKey = reasonKey;
        }

        public bool IsAllowed { get; }

        /// <summary>
        /// Localization key of the message explaining why the placement is denied.
        /// </summary>
        public string ReasonKey { get; }

        public static RuleVerdict Allow() => new RuleVerdict(true, null);

        public static RuleVerdict Deny(string reasonKey) => new RuleVerdict(false, reasonKey);
    }
}
