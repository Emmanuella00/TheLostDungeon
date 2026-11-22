using System.Collections.Generic;
using UnityEngine;

namespace TheLostDungeon.Algorithms
{
    /// <summary>
    /// ALGORITHM (gameplay logic, RECURSIVE): explosive barrel chain reactions.
    ///
    /// Purpose: when one barrel explodes, every barrel within range of it also explodes, and every
    /// barrel within range of THOSE explodes too, and so on.
    ///
    /// Approach: a recursive depth-first search (DFS). Barrels are nodes; two barrels are
    /// connected if they're within chainRadius of each other.
    ///   Explode(barrel):
    ///       base cases     -> stop if the barrel already exploded, or the chain is too deep
    ///       recursive case -> mark it exploded, then call Explode on every unexploded neighbour
    ///
    /// The "visited" flags are what guarantee termination: without them barrel A would set off
    /// B, B would set off A again, forever, until the call stack overflowed. maxDepth is an
    /// extra safety limit on the recursion depth.
    ///
    /// Returns the barrels in the order they go off, so the game can play the explosions one
    /// after another for a ripple effect. O(n²) for n barrels, fine for the handful in a room.
    /// </summary>
    public static class ChainReaction
    {
        public static List<int> FindChain(int startIndex, IReadOnlyList<Vector3> positions, float chainRadius, int maxDepth = 32)
        {
            var order = new List<int>();
            if (positions == null || startIndex < 0 || startIndex >= positions.Count) return order;

            var exploded = new bool[positions.Count];
            Explode(startIndex, 0, positions, chainRadius * chainRadius, maxDepth, exploded, order);
            return order;
        }

        private static void Explode(int index, int depth, IReadOnlyList<Vector3> positions, float radiusSqr,
                                    int maxDepth, bool[] exploded, List<int> order)
        {
            // Base cases: already gone, or recursion too deep.
            if (exploded[index] || depth > maxDepth) return;

            exploded[index] = true;
            order.Add(index);

            // Recursive case: every neighbour in range explodes too.
            for (int other = 0; other < positions.Count; other++)
            {
                if (exploded[other]) continue;
                if ((positions[other] - positions[index]).sqrMagnitude <= radiusSqr)
                {
                    Explode(other, depth + 1, positions, radiusSqr, maxDepth, exploded, order);
                }
            }
        }
    }
}
