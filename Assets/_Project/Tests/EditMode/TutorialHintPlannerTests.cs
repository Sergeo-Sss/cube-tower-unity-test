using CubeTower.Core.Config;
using CubeTower.Core.Tower;
using CubeTower.Core.Tutorial;
using NUnit.Framework;
using UnityEngine;

namespace CubeTower.Tests
{
    public sealed class TutorialHintPlannerTests
    {
        private static readonly TowerArea Area = new TowerArea(new Vector2(1000f, 450f), 100f);

        private TowerModel _tower;
        private TutorialHintPlanner _planner;

        [SetUp]
        public void SetUp()
        {
            _tower = new TowerModel(new TowerRulesConfig());
            _planner = new TutorialHintPlanner();
        }

        [TearDown]
        public void TearDown() => _tower.Dispose();

        [Test]
        public void EmptyTower_TeachesToPlaceFirstCube()
        {
            Assert.AreEqual(TutorialHint.PlaceFirstCube, _planner.Next(_tower, Area));
        }

        [Test]
        public void GrowingTower_AlternatesStackAndDiscard()
        {
            _tower.PlaceBase(Colors.Red, Vector2.zero);

            Assert.AreEqual(TutorialHint.StackCube, _planner.Next(_tower, Area));
            Assert.AreEqual(TutorialHint.DiscardCube, _planner.Next(_tower, Area));
            Assert.AreEqual(TutorialHint.StackCube, _planner.Next(_tower, Area));
        }

        [Test]
        public void FullTower_TeachesOnlyDiscard()
        {
            _tower.PlaceBase(Colors.Red, Vector2.zero);
            for (var i = 0; i < 3; i++)
                _tower.Stack(Colors.Red, 0f);

            Assert.AreEqual(TutorialHint.DiscardCube, _planner.Next(_tower, Area));
            Assert.AreEqual(TutorialHint.DiscardCube, _planner.Next(_tower, Area));
        }

        [Test]
        public void InvalidArea_NoHint()
        {
            Assert.AreEqual(TutorialHint.None, _planner.Next(_tower, new TowerArea(Vector2.zero, 100f)));
        }
    }
}
