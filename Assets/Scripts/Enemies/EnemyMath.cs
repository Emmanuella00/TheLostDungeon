using UnityEngine;

namespace TheLostDungeon.Enemies
{
    public enum RangeDecision
    {
        Approach,   // too far: move closer
        Retreat,    // too close: back away (Archers)
        Hold        // in the sweet spot: stop and attack
    }

    /// <summary>
    /// The maths behind enemy perception and positioning, kept pure so it can be unit tested.
    /// </summary>
    public static class EnemyMath
    {
        /// <summary>
        /// Is the target inside the view cone? Distance check with squared lengths, angle check
        /// with a dot product against cos(half angle), so no square roots or acos per frame.
        /// (Line of sight through walls is checked separately with a raycast.)
        /// </summary>
        public static bool IsInViewCone(Vector3 eye, Vector3 forward, Vector3 target, float range, float fullAngleDegrees)
        {
            Vector3 toTarget = target - eye;
            float distanceSqr = toTarget.sqrMagnitude;
            if (distanceSqr > range * range) return false;
            if (distanceSqr < 0.0001f) return true;
            if (fullAngleDegrees >= 360f) return true;

            float minDot = Mathf.Cos(fullAngleDegrees * 0.5f * Mathf.Deg2Rad);
            return Vector3.Dot(forward.normalized, toTarget / Mathf.Sqrt(distanceSqr)) >= minDot;
        }

        /// <summary>
        /// Decides how to move relative to the target. The SAME rule gives every enemy type its
        /// style: Warriors (min 0) always close in, Archers (min 8) keep their distance.
        /// </summary>
        public static RangeDecision DecideRange(float distance, float minDistance, float attackRange)
        {
            if (minDistance > 0f && distance < minDistance) return RangeDecision.Retreat;
            if (distance > attackRange) return RangeDecision.Approach;
            return RangeDecision.Hold;
        }

        /// <summary>Can the enemy hear a noise? Both the noise's reach and the enemy's hearing must cover it.</summary>
        public static bool CanHear(Vector3 listener, Vector3 noise, float noiseRadius, float hearingRange)
        {
            float reach = Mathf.Min(noiseRadius, hearingRange);
            return (noise - listener).sqrMagnitude <= reach * reach;
        }
    }
}
