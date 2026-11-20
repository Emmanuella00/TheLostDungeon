using UnityEngine;

namespace TheLostDungeon.Combat
{
    public enum DamageType
    {
        Ranged,     // blaster bolts, archer shots
        Melee,      // gun bash, warrior strikes
        Explosion   // barrels, boss attacks
    }

    /// <summary>
    /// Everything a hit carries. Built by the attacker, passed to IDamageable.TakeDamage.
    /// A struct so creating one per hit allocates no garbage.
    /// </summary>
    public readonly struct DamageInfo
    {
        public readonly float Amount;
        public readonly DamageType Type;
        public readonly Vector3 Point;
        public readonly Vector3 Direction;      // direction the hit is travelling
        public readonly float Knockback;
        public readonly GameObject Source;

        public DamageInfo(float amount, DamageType type, Vector3 point, Vector3 direction, float knockback = 0f, GameObject source = null)
        {
            Amount = amount;
            Type = type;
            Point = point;
            Direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector3.forward;
            Knockback = knockback;
            Source = source;
        }
    }

    /// <summary>What actually happened after defences were applied.</summary>
    public readonly struct DamageResult
    {
        public readonly float FinalAmount;
        public readonly bool Blocked;       // a shield absorbed (most of) the hit
        public readonly bool GuardBroken;   // a melee hit broke a shield guard

        public DamageResult(float finalAmount, bool blocked, bool guardBroken)
        {
            FinalAmount = finalAmount;
            Blocked = blocked;
            GuardBroken = guardBroken;
        }
    }
}
