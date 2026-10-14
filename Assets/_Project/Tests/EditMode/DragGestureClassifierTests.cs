using CubeTower.Core.Gestures;
using NUnit.Framework;
using UnityEngine;

namespace CubeTower.Tests
{
    public sealed class DragGestureClassifierTests
    {
        private readonly DragGestureClassifier _classifier = new DragGestureClassifier(30f, 40f, 0.2f);

        [Test]
        public void ShortMovement_IsUndecided()
        {
            Assert.AreEqual(DragGesture.Undecided, _classifier.Classify(new Vector2(20f, 5f), 0f));
            Assert.AreEqual(DragGesture.Undecided, _classifier.Classify(new Vector2(0f, 30f), 0f));
        }

        [TestCase(0f, 50f)]
        [TestCase(40f, 30f)] // ~37 degrees - diagonal pull towards the tower
        [TestCase(-40f, 30f)]
        public void SteepMovement_PicksCube(float x, float y)
        {
            Assert.AreEqual(DragGesture.PickCube, _classifier.Classify(new Vector2(x, y), 0f));
        }

        [TestCase(60f, 10f)]
        [TestCase(-80f, 20f)]
        public void FlatMovement_Scrolls(float x, float y)
        {
            Assert.AreEqual(DragGesture.Scroll, _classifier.Classify(new Vector2(x, y), 0f));
        }

        [Test]
        public void HoldBeforeMove_PicksCubeInAnyDirection()
        {
            Assert.AreEqual(DragGesture.PickCube, _classifier.Classify(new Vector2(60f, 0f), 0.25f));
            Assert.AreEqual(DragGesture.PickCube, _classifier.Classify(new Vector2(5f, 0f), 0.2f));
        }

        [Test]
        public void HoldDisabled_UsesDirectionOnly()
        {
            var classifier = new DragGestureClassifier(30f, 40f, 0f);

            Assert.AreEqual(DragGesture.Scroll, classifier.Classify(new Vector2(60f, 0f), 5f));
        }
    }
}
