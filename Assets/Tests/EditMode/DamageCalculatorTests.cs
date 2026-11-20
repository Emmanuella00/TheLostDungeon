using NUnit.Framework;
using TheLostDungeon.Combat;
using UnityEngine;

namespace TheLostDungeon.Tests
{
    /// <summary>Covers the brief's "Damage calculations" test area.</summary>
    public class DamageCalculatorTests
    {
        // The target faces +Z. A hit travelling toward -Z comes from in front of it.
        private static readonly Vector3 TargetForward = Vector3.forward;
        private static readonly Vector3 FromFront = Vector3.back;
        private static readonly Vector3 FromBehind = Vector3.forward;

        private static DamageInfo Hit(float amount, DamageType type, Vector3 direction) =>
            new DamageInfo(amount, type, Vector3.zero, direction);

        [Test]
        public void NoDefence_DealsFullDamage()
        {
            DamageResult result = DamageCalculator.Calculate(Hit(20f, DamageType.Ranged, FromFront), DefenceProfile.None, TargetForward);

            Assert.AreEqual(20f, result.FinalAmount, 0.001f);
            Assert.IsFalse(result.Blocked);
        }

        [Test]
        public void Armour_ReducesDamageByPercentage()
        {
            var defence = new DefenceProfile { armour = 0.25f };

            DamageResult result = DamageCalculator.Calculate(Hit(40f, DamageType.Melee, FromFront), defence, TargetForward);

            Assert.AreEqual(30f, result.FinalAmount, 0.001f);
        }

        [Test]
        public void FrontShield_BlocksRangedHitFromFront()
        {
            DefenceProfile shield = DefenceProfile.Shielded(blockFraction: 0.9f, arcDegrees: 120f);

            DamageResult result = DamageCalculator.Calculate(Hit(50f, DamageType.Ranged, FromFront), shield, TargetForward);

            Assert.IsTrue(result.Blocked);
            Assert.AreEqual(5f, result.FinalAmount, 0.001f);
        }

        [Test]
        public void FrontShield_DoesNotBlockRangedHitFromBehind()
        {
            DefenceProfile shield = DefenceProfile.Shielded(blockFraction: 0.9f, arcDegrees: 120f);

            DamageResult result = DamageCalculator.Calculate(Hit(50f, DamageType.Ranged, FromBehind), shield, TargetForward);

            Assert.IsFalse(result.Blocked);
            Assert.AreEqual(50f, result.FinalAmount, 0.001f);
        }

        [Test]
        public void MeleeOnFrontShield_IsNotBlocked_AndBreaksGuard()
        {
            DefenceProfile shield = DefenceProfile.Shielded(blockFraction: 1f, arcDegrees: 120f);

            DamageResult result = DamageCalculator.Calculate(Hit(45f, DamageType.Melee, FromFront), shield, TargetForward);

            Assert.IsFalse(result.Blocked);
            Assert.IsTrue(result.GuardBroken);
            Assert.AreEqual(45f, result.FinalAmount, 0.001f);
        }

        [Test]
        public void FullBlockShield_DealsZero_ButPartialBlockDealsAtLeastMinimum()
        {
            DamageResult full = DamageCalculator.Calculate(Hit(10f, DamageType.Ranged, FromFront), DefenceProfile.Shielded(1f), TargetForward);
            DamageResult tiny = DamageCalculator.Calculate(Hit(2f, DamageType.Ranged, FromFront), DefenceProfile.Shielded(0.9f), TargetForward);

            Assert.AreEqual(0f, full.FinalAmount, 0.001f);
            Assert.AreEqual(DamageCalculator.MinimumDamage, tiny.FinalAmount, 0.001f);
        }
    }
}
