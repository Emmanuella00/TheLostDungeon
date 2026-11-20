using UnityEngine;

namespace TheLostDungeon.Combat
{
    /// <summary>
    /// All damage rules in one place, as pure logic (no MonoBehaviour) so it can be unit tested.
    ///
    /// Rules, applied in order:
    ///   1. A front shield blocks RANGED hits that arrive inside its arc (Guardian bots).
    ///   2. A MELEE hit on a front shield is never blocked and "breaks the guard".
    ///   3. Armour reduces whatever is left by a percentage.
    ///   4. Any hit that isn't fully blocked deals at least MinimumDamage.
    ///
    /// Changing game balance (e.g. adding critical hits) means editing only this class.
    /// </summary>
    public static class DamageCalculator
    {
        public const float MinimumDamage = 1f;

        /// <param name="info">The incoming hit.</param>
        /// <param name="defence">The target's defences.</param>
        /// <param name="targetForward">Which way the target is facing (for shields).</param>
        public static DamageResult Calculate(in DamageInfo info, in DefenceProfile defence, Vector3 targetForward)
        {
            if (info.Amount <= 0f) return new DamageResult(0f, false, false);

            float amount = info.Amount;
            bool blocked = false;
            bool guardBroken = false;

            if (defence.hasFrontShield && IsHitFromFront(info.Direction, targetForward, defence.shieldArcDegrees))
            {
                if (info.Type == DamageType.Ranged)
                {
                    amount *= 1f - Mathf.Clamp01(defence.shieldBlockFraction);
                    blocked = true;
                }
                else if (info.Type == DamageType.Melee)
                {
                    guardBroken = true;
                }
            }

            amount *= 1f - Mathf.Clamp(defence.armour, 0f, 0.9f);

            bool fullyBlocked = blocked && amount <= 0.0001f;
            if (!fullyBlocked) amount = Mathf.Max(amount, MinimumDamage);
            else amount = 0f;

            return new DamageResult(amount, blocked, guardBroken);
        }

        /// <summary>
        /// True when the hit comes from in front of the target. The hit direction points from
        /// the attacker toward the target, so a frontal hit points roughly AGAINST targetForward.
        /// </summary>
        public static bool IsHitFromFront(Vector3 hitDirection, Vector3 targetForward, float arcDegrees)
        {
            Vector3 towardAttacker = -hitDirection;
            towardAttacker.y = 0f;
            targetForward.y = 0f;
            if (towardAttacker.sqrMagnitude < 0.0001f || targetForward.sqrMagnitude < 0.0001f) return false;

            float angle = Vector3.Angle(targetForward, towardAttacker);
            return angle <= arcDegrees * 0.5f;
        }
    }
}
