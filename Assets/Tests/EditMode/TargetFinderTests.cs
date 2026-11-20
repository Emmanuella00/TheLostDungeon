using System.Collections.Generic;
using NUnit.Framework;
using TheLostDungeon.Algorithms;
using UnityEngine;

namespace TheLostDungeon.Tests
{
    /// <summary>Covers the brief's "Algorithm correctness" test area (searching).</summary>
    public class TargetFinderTests
    {
        private static readonly Vector3 Origin = Vector3.zero;
        private static readonly Vector3 Forward = Vector3.forward;

        [Test]
        public void ReturnsNearestTargetInsideCone()
        {
            var targets = new List<Vector3>
            {
                new Vector3(0f, 0f, 20f),   // far, straight ahead
                new Vector3(0.5f, 0f, 8f),  // near, slightly right -> expected
                new Vector3(0f, 0f, 12f),
            };

            int index = TargetFinder.FindNearestInCone(Origin, Forward, targets, 50f, 10f);

            Assert.AreEqual(1, index);
        }

        [Test]
        public void IgnoresTargetsOutsideCone_EvenIfCloser()
        {
            var targets = new List<Vector3>
            {
                new Vector3(3f, 0f, 3f),    // 45 degrees off, very close
                new Vector3(0f, 0f, 15f),   // straight ahead -> expected
            };

            int index = TargetFinder.FindNearestInCone(Origin, Forward, targets, 50f, 10f);

            Assert.AreEqual(1, index);
        }

        [Test]
        public void IgnoresTargetsBeyondRange_AndBehind()
        {
            var targets = new List<Vector3>
            {
                new Vector3(0f, 0f, 60f),   // too far
                new Vector3(0f, 0f, -5f),   // behind
            };

            Assert.AreEqual(-1, TargetFinder.FindNearestInCone(Origin, Forward, targets, 50f, 15f));
        }

        [Test]
        public void EmptyList_ReturnsMinusOne()
        {
            Assert.AreEqual(-1, TargetFinder.FindNearestInCone(Origin, Forward, new List<Vector3>(), 50f, 10f));
        }
    }
}
