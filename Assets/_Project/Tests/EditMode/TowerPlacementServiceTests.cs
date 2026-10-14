using System.Collections.Generic;
using CubeTower.Core.Config;
using CubeTower.Core.Messaging;
using CubeTower.Core.Tower;
using CubeTower.Core.Tower.Placement;
using NUnit.Framework;
using UnityEngine;

namespace CubeTower.Tests
{
    public sealed class TowerPlacementServiceTests
    {
        private const float CubeSize = 100f;
        private static readonly TowerArea Area = new TowerArea(new Vector2(1000f, 450f), CubeSize);

        private TowerModel _tower;

        [TearDown]
        public void TearDown() => _tower?.Dispose();

        [Test]
        public void EmptyTower_DropInsideArea_PlacesBaseAtDropPoint()
        {
            var service = Create();

            var result = service.Evaluate(Colors.Red, new Vector2(200f, 300f), Area);

            Assert.AreEqual(PlacementStatus.Accepted, result.Status);
            Assert.AreEqual(PlacementKind.Base, result.Candidate.Kind);
            Assert.AreEqual(TowerLayout.ToBasePosition(new Vector2(200f, 300f), Area), result.Candidate.BasePosition);
        }

        [Test]
        public void EmptyTower_DropOutsideArea_Missed()
        {
            var service = Create();

            Assert.AreEqual(PlacementStatus.Missed, service.Evaluate(Colors.Red, new Vector2(-10f, 100f), Area).Status);
            Assert.AreEqual(PlacementStatus.Missed, service.Evaluate(Colors.Red, new Vector2(100f, 460f), Area).Status);
        }

        [Test]
        public void DropOverTower_StacksWithOffsetWithinHalfEdge()
        {
            var service = Create(random: 1f);
            service.Apply(service.Evaluate(Colors.Red, new Vector2(500f, 50f), Area).Candidate);

            var result = service.Evaluate(Colors.Green, new Vector2(540f, 120f), Area);

            Assert.AreEqual(PlacementStatus.Accepted, result.Status);
            Assert.AreEqual(PlacementKind.Stack, result.Candidate.Kind);
            Assert.AreEqual(0.5f, result.Candidate.Offset, 1e-5f);
        }

        [Test]
        public void DropAwayFromTower_Missed()
        {
            var service = Create();
            service.Apply(service.Evaluate(Colors.Red, new Vector2(500f, 50f), Area).Candidate);

            var result = service.Evaluate(Colors.Green, new Vector2(800f, 300f), Area);

            Assert.AreEqual(PlacementStatus.Missed, result.Status);
        }

        [Test]
        public void StackOffset_KeepsCubeInsideArea()
        {
            var service = Create(random: 1f);
            // Base at the right border: any positive offset would push the next cube out of the area.
            service.Apply(service.Evaluate(Colors.Red, new Vector2(950f, 50f), Area).Candidate);

            var result = service.Evaluate(Colors.Green, new Vector2(950f, 120f), Area);

            Assert.AreEqual(PlacementStatus.Accepted, result.Status);
            Assert.LessOrEqual(result.Candidate.Offset, 0f);
        }

        [Test]
        public void TowerReachedCeiling_RejectedWithHeightLimit()
        {
            var service = Create();
            service.Apply(service.Evaluate(Colors.Red, new Vector2(500f, 50f), Area).Candidate);
            for (var i = 0; i < 3; i++)
                service.Apply(service.Evaluate(Colors.Red, new Vector2(500f, 100f), Area).Candidate);

            // 4 cubes * 100 = 400 <= 450, the fifth one would end at 500.
            var result = service.Evaluate(Colors.Red, new Vector2(500f, 350f), Area);

            Assert.AreEqual(PlacementStatus.Rejected, result.Status);
            Assert.AreEqual(MessageKeys.HeightLimit, result.ReasonKey);
        }

        [Test]
        public void SameColorRule_DifferentColor_Rejected()
        {
            var service = Create(requireSameColor: true);
            service.Apply(service.Evaluate(Colors.Red, new Vector2(500f, 50f), Area).Candidate);

            var mismatch = service.Evaluate(Colors.Green, new Vector2(500f, 100f), Area);
            var match = service.Evaluate(Colors.Red, new Vector2(500f, 100f), Area);

            Assert.AreEqual(PlacementStatus.Rejected, mismatch.Status);
            Assert.AreEqual(MessageKeys.ColorMismatch, mismatch.ReasonKey);
            Assert.AreEqual(PlacementStatus.Accepted, match.Status);
        }

        [Test]
        public void InvalidArea_Missed()
        {
            var service = Create();

            var result = service.Evaluate(Colors.Red, Vector2.zero, new TowerArea(Vector2.zero, CubeSize));

            Assert.AreEqual(PlacementStatus.Missed, result.Status);
        }

        private TowerPlacementService Create(float random = 0.5f, bool requireSameColor = false)
        {
            var rules = new TowerRulesConfig(0.5f, requireSameColor);
            _tower = new TowerModel(rules);
            var placementRules = new List<IPlacementRule> { new HeightLimitRule(), new SameColorRule(rules) };
            return new TowerPlacementService(_tower, placementRules, new FixedRandom(random));
        }
    }
}
