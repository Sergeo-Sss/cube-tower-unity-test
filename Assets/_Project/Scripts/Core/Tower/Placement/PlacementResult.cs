namespace CubeTower.Core.Tower.Placement
{
    public enum PlacementStatus
    {
        /// <summary>The cube can be placed.</summary>
        Accepted,

        /// <summary>The cube was dropped neither into the empty area nor onto the tower.</summary>
        Missed,

        /// <summary>The cube hit the tower but one of the rules denied the placement.</summary>
        Rejected
    }

    public readonly struct PlacementResult
    {
        private PlacementResult(PlacementStatus status, PlacementCandidate candidate, string reasonKey)
        {
            Status = status;
            Candidate = candidate;
            ReasonKey = reasonKey;
        }

        public PlacementStatus Status { get; }

        public PlacementCandidate Candidate { get; }

        public string ReasonKey { get; }

        public static PlacementResult Accepted(PlacementCandidate candidate) =>
            new PlacementResult(PlacementStatus.Accepted, candidate, null);

        public static PlacementResult Missed() => new PlacementResult(PlacementStatus.Missed, default, null);

        public static PlacementResult Rejected(PlacementCandidate candidate, string reasonKey) =>
            new PlacementResult(PlacementStatus.Rejected, candidate, reasonKey);
    }
}
