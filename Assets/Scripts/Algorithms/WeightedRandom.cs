using System.Collections.Generic;

namespace TheLostDungeon.Algorithms
{
    /// <summary>
    /// ALGORITHM (gameplay logic): weighted random selection, also called "roulette wheel" selection.
    ///
    /// Purpose: chest loot. Each entry has a weight (credits 60, health pack 30, rare item 10) and
    /// the chance of picking it is weight / total weight.
    ///
    /// Approach: imagine the weights laid end to end on a line of length = total. Pick a point on
    /// that line (random01 * total) and walk the entries, adding up weights (a running "cumulative"
    /// total), until the running total passes the point. O(n) for n entries.
    ///
    /// Why: it's simple, needs no extra memory, and designers can tune drop rates by editing
    /// weights in the Inspector. The random value is passed in (instead of calling Random inside)
    /// so tests can check exact results.
    /// </summary>
    public static class WeightedRandom
    {
        /// <param name="weights">Weights per entry. Zero or negative weights can never be picked.</param>
        /// <param name="random01">A random number from 0 to 1 (e.g. UnityEngine.Random.value).</param>
        /// <returns>Chosen index, or -1 if no entry has a positive weight.</returns>
        public static int PickIndex(IReadOnlyList<float> weights, float random01)
        {
            if (weights == null || weights.Count == 0) return -1;

            float total = 0f;
            int lastValid = -1;
            for (int i = 0; i < weights.Count; i++)
            {
                if (weights[i] > 0f)
                {
                    total += weights[i];
                    lastValid = i;
                }
            }
            if (total <= 0f) return -1;

            float point = (random01 < 0f ? 0f : random01 > 1f ? 1f : random01) * total;
            float cumulative = 0f;

            for (int i = 0; i < weights.Count; i++)
            {
                if (weights[i] <= 0f) continue;
                cumulative += weights[i];
                if (point < cumulative) return i;
            }

            return lastValid;   // random01 == 1 lands exactly on the end of the line
        }
    }
}
