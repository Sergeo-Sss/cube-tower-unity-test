using System.Collections.Generic;
using CubeTower.Core.Config;
using CubeTower.Core.Cubes;
using CubeTower.Core.Geometry;
using CubeTower.Core.Progress;
using CubeTower.Core.Tower;
using NUnit.Framework;
using UnityEngine;

namespace CubeTower.Tests
{
    public sealed class TowerProgressMapperTests
    {
        [Test]
        public void CaptureAndRestore_RoundTripThroughJson()
        {
            var source = new TowerModel(new TowerRulesConfig());
            source.PlaceBase(Colors.Red, new Vector2(1.5f, 0.25f));
            source.Stack(Colors.Green, 0.3f);
            source.Stack(Colors.Blue, -0.2f);

            var json = JsonUtility.ToJson(TowerProgressMapper.Capture(source));
            var restored = new TowerModel(new TowerRulesConfig());
            var ok = TowerProgressMapper.TryRestore(restored, JsonUtility.FromJson<TowerProgressData>(json), TestConfigs.Default());

            Assert.IsTrue(ok);
            Assert.AreEqual(source.BasePosition, restored.BasePosition);
            Assert.AreEqual(source.Count, restored.Count);
            for (var i = 0; i < source.Count; i++)
            {
                Assert.AreEqual(source.Cubes[i].Color, restored.Cubes[i].Color);
                Assert.AreEqual(source.Cubes[i].Offset, restored.Cubes[i].Offset, 1e-5f);
            }
        }

        [Test]
        public void Restore_UnknownColors_AreSkippedAndShapeIsKept()
        {
            var data = new TowerProgressData
            {
                BaseX = 1f,
                Cubes = new List<TowerCubeData>
                {
                    new TowerCubeData { ColorId = "removed_color", Offset = 0f },
                    new TowerCubeData { ColorId = "red", Offset = 0.2f },
                    new TowerCubeData { ColorId = null, Offset = 0.1f },
                    new TowerCubeData { ColorId = "green", Offset = 0.1f }
                }
            };
            var tower = new TowerModel(new TowerRulesConfig());

            Assert.IsTrue(TowerProgressMapper.TryRestore(tower, data, TestConfigs.Default()));

            Assert.AreEqual(2, tower.Count);
            Assert.AreEqual(1.2f, tower.BasePosition.x, 1e-5f);
            Assert.AreEqual(0.2f, tower.Cubes[1].Offset, 1e-5f);
        }

        [Test]
        public void Restore_CorruptedData_IsRejected()
        {
            var tower = new TowerModel(new TowerRulesConfig());
            var config = TestConfigs.Default();

            Assert.IsFalse(TowerProgressMapper.TryRestore(tower, null, config));
            Assert.IsFalse(TowerProgressMapper.TryRestore(tower, new TowerProgressData { Version = 999 }, config));
            Assert.IsFalse(TowerProgressMapper.TryRestore(tower, new TowerProgressData { BaseX = float.NaN }, config));
        }

        [Test]
        public void Restore_NullCubesList_RestoresEmptyTower()
        {
            var tower = new TowerModel(new TowerRulesConfig());
            tower.PlaceBase(Colors.Red, Vector2.zero);

            Assert.IsTrue(TowerProgressMapper.TryRestore(tower, new TowerProgressData { Cubes = null }, TestConfigs.Default()));
            Assert.IsTrue(tower.IsEmpty);
        }
    }

    public sealed class GameConfigTests
    {
        [Test]
        public void Create_CyclesColorsUpToCount()
        {
            var config = GameConfig.Create(5, new[] { Colors.Red, Colors.Green }, new TowerRulesConfig());

            CollectionAssert.AreEqual(new[] { Colors.Red, Colors.Green, Colors.Red, Colors.Green, Colors.Red }, config.PaletteCubes);
            Assert.AreEqual(2, config.Colors.Count);
        }

        [Test]
        public void Create_InvalidInput_Throws()
        {
            Assert.Throws<GameConfigException>(() => GameConfig.Create(0, new[] { Colors.Red }, new TowerRulesConfig()));
            Assert.Throws<GameConfigException>(() => GameConfig.Create(3, new CubeColorId[0], new TowerRulesConfig()));
        }

        [Test]
        public void TowerRules_ClampsOffset()
        {
            Assert.AreEqual(1f, new TowerRulesConfig(5f).MaxHorizontalOffset);
            Assert.AreEqual(TowerRulesConfig.DefaultMaxHorizontalOffset, new TowerRulesConfig(float.NaN).MaxHorizontalOffset);
        }

        [Test]
        public void CubeColorId_EmptyValue_IsRejected()
        {
            Assert.IsFalse(CubeColorId.TryCreate("  ", out _));
            Assert.Throws<System.ArgumentException>(() => new CubeColorId(null));
            Assert.AreEqual(new CubeColorId("red"), new CubeColorId(" red "));
        }
    }

    public sealed class EllipseTests
    {
        private static readonly Ellipse Hole = Ellipse.FromRect(new Rect(-331f, -97.5f, 662f, 195f));

        [TestCase(0f, 0f, ExpectedResult = true)]
        [TestCase(330f, 0f, ExpectedResult = true)]
        [TestCase(0f, 97f, ExpectedResult = true)]
        [TestCase(300f, 80f, ExpectedResult = false)] // inside the bounding rect, outside the oval
        [TestCase(-331f, 97.5f, ExpectedResult = false)]
        [TestCase(0f, 120f, ExpectedResult = false)]
        public bool Contains_RespectsOvalShape(float x, float y) => Hole.Contains(new Vector2(x, y));

        [Test]
        public void DegenerateEllipse_ContainsNothing()
        {
            Assert.IsFalse(new Ellipse(Vector2.zero, Vector2.zero).Contains(Vector2.zero));
        }
    }
}
