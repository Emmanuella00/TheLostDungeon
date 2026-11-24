using System;
using System.Collections.Generic;

namespace TheLostDungeon.Algorithms
{
    /// <summary>
    /// ALGORITHM (sorting): merge sort, used to rank leaderboard entries.
    ///
    /// Approach (divide and conquer, top-down, recursive):
    ///   1. Split the list into two halves.
    ///   2. Sort each half (recursively, until a half has 0 or 1 items, which is already sorted).
    ///   3. Merge the two sorted halves by repeatedly taking the smaller front item.
    ///
    /// Complexity: O(n log n) time in every case (best, average, worst), O(n) extra memory
    /// for the merge buffer.
    ///
    /// Why merge sort here: it is STABLE, meaning items that compare equal keep their original
    /// order. The leaderboard sorts by score, and for equal scores the entry that was submitted
    /// FIRST should stay above, so stability matters. (List.Sort is an unstable introsort.)
    /// It also never degrades to O(n²) like quicksort can on bad input.
    /// </summary>
    public static class MergeSort
    {
        /// <summary>Sorts the list in place. Stable.</summary>
        public static void Sort<T>(IList<T> items, Comparison<T> compare)
        {
            if (items == null || items.Count < 2) return;
            if (compare == null) throw new ArgumentNullException(nameof(compare));

            var buffer = new T[items.Count];
            SortRange(items, buffer, 0, items.Count - 1, compare);
        }

        /// <summary>Returns a sorted copy and leaves the input untouched.</summary>
        public static List<T> Sorted<T>(IEnumerable<T> items, Comparison<T> compare)
        {
            var copy = new List<T>(items);
            Sort(copy, compare);
            return copy;
        }

        private static void SortRange<T>(IList<T> items, T[] buffer, int left, int right, Comparison<T> compare)
        {
            if (left >= right) return;                 // 0 or 1 item: already sorted (base case)

            int middle = left + (right - left) / 2;    // avoids int overflow on huge lists
            SortRange(items, buffer, left, middle, compare);
            SortRange(items, buffer, middle + 1, right, compare);

            // Already in order? Skip the merge (makes nearly-sorted input fast).
            if (compare(items[middle], items[middle + 1]) <= 0) return;

            Merge(items, buffer, left, middle, right, compare);
        }

        private static void Merge<T>(IList<T> items, T[] buffer, int left, int middle, int right, Comparison<T> compare)
        {
            int i = left, j = middle + 1, k = left;

            while (i <= middle && j <= right)
            {
                // "<=" takes from the LEFT half on ties: this is what makes the sort stable.
                if (compare(items[i], items[j]) <= 0) buffer[k++] = items[i++];
                else buffer[k++] = items[j++];
            }
            while (i <= middle) buffer[k++] = items[i++];
            while (j <= right) buffer[k++] = items[j++];

            for (k = left; k <= right; k++) items[k] = buffer[k];
        }
    }
}
