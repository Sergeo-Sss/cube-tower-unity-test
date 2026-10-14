using System.Collections.Generic;
using CubeTower.Core.Config;
using CubeTower.Core.Cubes;
using CubeTower.Core.Tower;
using NUnit.Framework;
using UniRx;
using UnityEngine;

namespace CubeTower.Tests
{
    public sealed class TowerModelTests
    {
        private TowerModel _tower;

        [SetUp]
        public void SetUp() => _tower = new TowerModel(new TowerRulesConfig(0.5f));

        [TearDown]
        public void TearDown() => _tower.Dispose();

        [Test]
        public void PlaceBase_EmptyTower_AddsCubeAtPosition()
        {
            var cube = _tower.PlaceBase(Colors.Red, new Vector2(1f, 2f));

            Assert.AreEqual(1, _tower.Count);
            Assert.AreSame(cube, _tower.Top);
            Assert.AreEqual(new Vector2(1f, 2f), _tower.BasePosition);
            Assert.AreEqual(0f, cube.Offset);
        }

        [Test]
        public void PlaceBase_NotEmptyTower_Throws()
        {
            _tower.PlaceBase(Colors.Red, Vector2.zero);

            Assert.Throws<System.InvalidOperationException>(() => _tower.PlaceBase(Colors.Red, Vector2.zero));
        }

        [Test]
        public void Stack_EmptyTower_Throws()
        {
            Assert.Throws<System.InvalidOperationException>(() => _tower.Stack(Colors.Red, 0f));
        }

        [Test]
        public void Stack_OffsetOutOfRange_IsClamped()
        {
            _tower.PlaceBase(Colors.Red, Vector2.zero);

            Assert.AreEqual(0.5f, _tower.Stack(Colors.Green, 3f).Offset);
            Assert.AreEqual(-0.5f, _tower.Stack(Colors.Green, -3f).Offset);
            Assert.AreEqual(0f, _tower.Stack(Colors.Green, float.NaN).Offset);
        }

        [Test]
        public void PlaceBase_NegativeY_IsClampedToGround()
        {
            _tower.PlaceBase(Colors.Red, new Vector2(0f, -5f));

            Assert.AreEqual(0f, _tower.BasePosition.y);
        }

        [Test]
        public void TryRemove_Middle_MergesOffsetsOfNeighbours()
        {
            _tower.PlaceBase(Colors.Red, Vector2.zero);
            var middle = _tower.Stack(Colors.Green, 0.2f);
            var top = _tower.Stack(Colors.Blue, 0.1f);

            Assert.IsTrue(_tower.TryRemove(middle.Id, out _));

            Assert.AreEqual(2, _tower.Count);
            Assert.AreEqual(0.3f, top.Offset, 1e-5f);
        }

        [Test]
        public void TryRemove_Middle_MergedOffsetIsClamped()
        {
            _tower.PlaceBase(Colors.Red, Vector2.zero);
            var middle = _tower.Stack(Colors.Green, 0.4f);
            var top = _tower.Stack(Colors.Blue, 0.4f);

            _tower.TryRemove(middle.Id, out _);

            Assert.AreEqual(0.5f, top.Offset, 1e-5f);
        }

        [Test]
        public void TryRemove_Base_NextCubeBecomesBaseAndKeepsHorizontalPosition()
        {
            _tower.PlaceBase(Colors.Red, new Vector2(1f, 0.5f));
            var base1 = _tower.Cubes[0];
            var second = _tower.Stack(Colors.Green, 0.3f);

            _tower.TryRemove(base1.Id, out _);

            Assert.AreSame(second, _tower.Cubes[0]);
            Assert.AreEqual(0f, second.Offset);
            Assert.AreEqual(1.3f, _tower.BasePosition.x, 1e-5f);
            Assert.AreEqual(0.5f, _tower.BasePosition.y, 1e-5f);
        }

        [Test]
        public void TryRemove_UnknownId_ReturnsFalse()
        {
            _tower.PlaceBase(Colors.Red, Vector2.zero);

            Assert.IsFalse(_tower.TryRemove(-1, out var removed));
            Assert.IsNull(removed);
            Assert.AreEqual(1, _tower.Count);
        }

        [Test]
        public void Changes_AreReportedForAllOperations()
        {
            var changes = new List<TowerChange>();
            using (_tower.Changes.Subscribe(changes.Add))
            {
                var cube = _tower.PlaceBase(Colors.Red, Vector2.zero);
                _tower.Stack(Colors.Green, 0f);
                _tower.TryRemove(cube.Id, out _);
                _tower.Restore(Vector2.zero, new List<(CubeColorId, float)> { (Colors.Blue, 0f) });
            }

            CollectionAssert.AreEqual(
                new[] { TowerChangeType.Added, TowerChangeType.Added, TowerChangeType.Removed, TowerChangeType.Reset },
                changes.ConvertAll(change => change.Type));
            Assert.AreEqual(0, changes[2].Index);
        }

        [Test]
        public void Restore_ReplacesStateAndForcesBaseOffsetToZero()
        {
            _tower.PlaceBase(Colors.Red, Vector2.zero);

            _tower.Restore(new Vector2(2f, 1f), new List<(CubeColorId, float)> { (Colors.Green, 0.4f), (Colors.Blue, 0.9f) });

            Assert.AreEqual(2, _tower.Count);
            Assert.AreEqual(Colors.Green, _tower.Cubes[0].Color);
            Assert.AreEqual(0f, _tower.Cubes[0].Offset);
            Assert.AreEqual(0.5f, _tower.Cubes[1].Offset);
            Assert.AreEqual(new Vector2(2f, 1f), _tower.BasePosition);
        }
    }
}
