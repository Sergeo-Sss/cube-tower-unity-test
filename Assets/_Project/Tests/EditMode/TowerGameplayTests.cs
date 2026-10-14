using System.Collections.Generic;
using CubeTower.Core.Config;
using CubeTower.Core.Messaging;
using CubeTower.Core.Tower;
using CubeTower.Core.Tower.Placement;
using NUnit.Framework;
using UniRx;
using UnityEngine;

namespace CubeTower.Tests
{
    public sealed class TowerGameplayTests
    {
        private static readonly TowerArea Area = new TowerArea(new Vector2(1000f, 450f), 100f);

        private TowerModel _tower;
        private GameMessenger _messenger;
        private TowerGameplay _gameplay;
        private List<string> _messages;

        [SetUp]
        public void SetUp()
        {
            var rules = new TowerRulesConfig();
            _tower = new TowerModel(rules);
            _messenger = new GameMessenger();
            _messages = new List<string>();
            _messenger.Messages.Subscribe(message => _messages.Add(message.Key));
            var placement = new TowerPlacementService(_tower, new List<IPlacementRule> { new HeightLimitRule() }, new FixedRandom(0.5f));
            _gameplay = new TowerGameplay(_tower, placement, _messenger);
        }

        [TearDown]
        public void TearDown()
        {
            _messenger.Dispose();
            _tower.Dispose();
        }

        [Test]
        public void EveryAction_PublishesMessage()
        {
            var placed = _gameplay.DropNewCube(Colors.Red, new Vector2(500f, 50f), Area);
            var missed = _gameplay.DropNewCube(Colors.Red, new Vector2(900f, 400f), Area);
            for (var i = 0; i < 3; i++)
                _gameplay.DropNewCube(Colors.Red, new Vector2(500f, 100f), Area);
            var limited = _gameplay.DropNewCube(Colors.Red, new Vector2(500f, 100f), Area);
            _gameplay.DiscardFromTower(placed.Cube.Id);

            Assert.IsTrue(placed.IsPlaced);
            Assert.IsFalse(missed.IsPlaced);
            Assert.IsFalse(limited.IsPlaced);
            CollectionAssert.AreEqual(new[]
            {
                MessageKeys.CubePlaced,
                MessageKeys.CubeMissed,
                MessageKeys.CubePlaced,
                MessageKeys.CubePlaced,
                MessageKeys.CubePlaced,
                MessageKeys.HeightLimit,
                MessageKeys.CubeDiscarded
            }, _messages);
            Assert.AreEqual(3, _tower.Count);
        }

        [Test]
        public void DiscardFromTower_UnknownCube_ReturnsFalseWithoutMessage()
        {
            Assert.IsFalse(_gameplay.DiscardFromTower(42));
            CollectionAssert.IsEmpty(_messages);
        }
    }
}
