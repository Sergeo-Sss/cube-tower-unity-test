using CubeTower.Core.Tower;

namespace CubeTower.Core.Tutorial
{
    public enum TutorialHint
    {
        None,

        /// <summary>Drag a cube from the palette into the empty tower area.</summary>
        PlaceFirstCube,

        /// <summary>Drag a cube from the palette onto the top of the tower.</summary>
        StackCube,

        /// <summary>Drag the top cube of the tower into the hole.</summary>
        DiscardCube
    }

    /// <summary>
    /// Chooses what the idle hint should teach for the current state of the tower.
    /// While the tower can grow, stacking and discarding hints alternate so both mechanics are shown.
    /// </summary>
    public sealed class TutorialHintPlanner
    {
        private int _towerHintsShown;

        public TutorialHint Next(TowerModel tower, TowerArea area)
        {
            if (tower == null || !area.IsValid)
                return TutorialHint.None;

            if (tower.IsEmpty)
                return TutorialHint.PlaceFirstCube;

            if (!TowerLayout.Fits(tower.BasePosition.y, tower.Count + 1, area))
                return TutorialHint.DiscardCube;

            return _towerHintsShown++ % 2 == 0 ? TutorialHint.StackCube : TutorialHint.DiscardCube;
        }
    }
}
