using System.Collections.Generic;
using CubeTower.Core.Config;
using CubeTower.Core.Tower;
using NUnit.Framework;
using UnityEngine;

namespace CubeTower.Tests
{
    public sealed class TowerLayoutTests
    {
        private const float CubeSize = 100f;
        private static readonly TowerArea Area = new TowerArea(new Vector2(1000f, 800f), CubeSize);

        [Test]
        public void ToBasePosition_IsInverseOfLayout()
        {
            var tower = new TowerModel(new TowerRulesConfig());
            tower.PlaceBase(Colors.Red, TowerLayout.ToBasePosition(new Vector2(300f, 250f), Area));

            var centers = new List<Vector2>();
            TowerLayout.CalculateCenters(tower, Area, centers);

            Assert.AreEqual(300f, centers[0].x, 1e-3f);
            Assert.AreEqual(250f, centers[0].y, 1e-3f);
        }

        [Test]
        public void ToBasePosition_ClampsCubeInsideArea()
        {
            var position = TowerLayout.ToBasePosition(new Vector2(-50f, 5000f), Area);

            // Left edge touches the area border, top edge touches the ceiling.
            Assert.AreEqual((50f - 500f) / CubeSize, position.x, 1e-5f);
            Assert.AreEqual((800f - CubeSize) / CubeSize, position.y, 1e-5f);
        }

        [Test]
        public void CalculateCenters_StacksCubesWithOffsets()
        {
            var tower = new TowerModel(new TowerRulesConfig());
            tower.PlaceBase(Colors.Red, Vector2.zero);
            tower.Stack(Colors.Green, 0.5f);
            tower.Stack(Colors.Blue, -0.25f);

            var centers = new List<Vector2>();
            TowerLayout.CalculateCenters(tower, Area, centers);

            Assert.AreEqual(new Vector2(500f, 50f), centers[0]);
            Assert.AreEqual(new Vector2(550f, 150f), centers[1]);
            Assert.AreEqual(new Vector2(525f, 250f), centers[2]);
        }

        [Test]
        public void CalculateCenters_TowerOutsideNarrowArea_IsShiftedInside()
        {
            var tower = new TowerModel(new TowerRulesConfig());
            tower.PlaceBase(Colors.Red, new Vector2(4.5f, 0f)); // right border of a 1000 wide area
            tower.Stack(Colors.Green, 0.5f);

            var narrow = new TowerArea(new Vector2(600f, 800f), CubeSize);
            var centers = new List<Vector2>();
            TowerLayout.CalculateCenters(tower, narrow, centers);

            Assert.LessOrEqual(centers[1].x + CubeSize * 0.5f, 600f + 1e-3f);
            Assert.AreEqual(CubeSize * 0.5f, centers[1].x - centers[0].x, 1e-3f, "Relative offsets must be preserved.");
        }
    }
}
