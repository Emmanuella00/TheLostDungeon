using System.Collections.Generic;
using UnityEngine;

namespace TheLostDungeon.Algorithms
{
    /// <summary>
    /// ALGORITHM (searching): nearest target inside a view cone.
    ///
    /// Purpose: aim assist and lock-on. Finds the closest valid target that is in front of the
    /// player (within maxAngle of the aim direction) and within range.
    ///
    /// Approach: a single linear scan, O(n). For each candidate it
    ///   1. rejects it if it's beyond range (compares SQUARED distances, which avoids a square
    ///      root per enemy),
    ///   2. rejects it if it's outside the cone (dot product against cos(maxAngle), so no
    ///      Vector3.Angle/acos call per enemy),
    ///   3. keeps it if it's closer than the best so far.
    ///
    /// Why this algorithm: a level only has tens of enemies, so a linear scan is faster and
    /// simpler than building a spatial structure (k-d tree, grid) every frame.
    /// </summary>
    public static class TargetFinder
    {
        /// <returns>Index of the nearest valid candidate, or -1 if none qualifies.</returns>
        public static int FindNearestInCone(Vector3 origin, Vector3 forward, IReadOnlyList<Vector3> candidates,
                                            float maxDistance, float maxAngleDegrees)
        {
            if (candidates == null || candidates.Count == 0 || maxDistance <= 0f) return -1;

            Vector3 aim = forward.sqrMagnitude > 0f ? forward.normalized : Vector3.forward;
            float maxDistanceSqr = maxDistance * maxDistance;
            float minDot = Mathf.Cos(Mathf.Clamp(maxAngleDegrees, 0f, 180f) * Mathf.Deg2Rad);

            int bestIndex = -1;
            float bestDistanceSqr = float.PositiveInfinity;

            for (int i = 0; i < candidates.Count; i++)
            {
                Vector3 toTarget = candidates[i] - origin;
                float distanceSqr = toTarget.sqrMagnitude;

                if (distanceSqr > maxDistanceSqr || distanceSqr < 0.0001f) continue;

                float dot = Vector3.Dot(aim, toTarget / Mathf.Sqrt(distanceSqr));
                if (dot < minDot) continue;

                if (distanceSqr < bestDistanceSqr)
                {
                    bestDistanceSqr = distanceSqr;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }
    }
}
