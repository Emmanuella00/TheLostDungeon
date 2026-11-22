using NUnit.Framework;
using TheLostDungeon.Enemies;
using UnityEngine;

namespace TheLostDungeon.Tests
{
    /// <summary>Covers "Mathematical calculations": enemy vision, hearing and positioning.</summary>
    public class EnemyMathTests
    {
        private static readonly Vector3 Eye = Vector3.zero;
        private static readonly Vector3 Forward = Vector3.forward;

        [Test]
        public void ViewCone_SeesTargetAheadWithinRange()
        {
            Assert.IsTrue(EnemyMath.IsInViewCone(Eye, Forward, new Vector3(1f, 0f, 10f), 16f, 110f));
        }

        [Test]
        public void ViewCone_IgnoresTargetBehindOrTooFar()
        {
            Assert.IsFalse(EnemyMath.IsInViewCone(Eye, Forward, new Vector3(0f, 0f, -5f), 16f, 110f), "behind");
            Assert.IsFalse(EnemyMath.IsInViewCone(Eye, Forward, new Vector3(0f, 0f, 20f), 16f, 110f), "too far");
        }

        [Test]
        public void ViewCone_EdgeOfAngle()
        {
            // 110 degree cone = 55 degrees each side. 50 degrees off is seen, 60 is not.
            Vector3 at50 = Quaternion.Euler(0f, 50f, 0f) * Vector3.forward * 5f;
            Vector3 at60 = Quaternion.Euler(0f, 60f, 0f) * Vector3.forward * 5f;
            Assert.IsTrue(EnemyMath.IsInViewCone(Eye, Forward, at50, 16f, 110f));
            Assert.IsFalse(EnemyMath.IsInViewCone(Eye, Forward, at60, 16f, 110f));
        }

        [TestCase(10f, 0f, 1.8f, RangeDecision.Approach)]   // Warrior far away: close in
        [TestCase(1.5f, 0f, 1.8f, RangeDecision.Hold)]      // Warrior in reach: attack
        [TestCase(4f, 8f, 14f, RangeDecision.Retreat)]      // Archer rushed: back off
        [TestCase(10f, 8f, 14f, RangeDecision.Hold)]        // Archer at a good distance: shoot
        [TestCase(20f, 8f, 14f, RangeDecision.Approach)]    // Archer too far: move in
        public void DecideRange_GivesEachTypeItsStyle(float distance, float min, float range, RangeDecision expected)
        {
            Assert.AreEqual(expected, EnemyMath.DecideRange(distance, min, range));
        }

        [Test]
        public void CanHear_UsesTheSmallerOfNoiseAndHearingRange()
        {
            Vector3 noise = new Vector3(0f, 0f, 15f);
            Assert.IsTrue(EnemyMath.CanHear(Vector3.zero, noise, 20f, 25f));
            Assert.IsFalse(EnemyMath.CanHear(Vector3.zero, noise, 10f, 25f), "quiet noise");
            Assert.IsFalse(EnemyMath.CanHear(Vector3.zero, noise, 20f, 12f), "deaf-ish enemy");
        }
    }
}
