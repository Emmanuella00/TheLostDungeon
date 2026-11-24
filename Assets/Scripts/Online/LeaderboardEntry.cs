using System;
using System.Collections.Generic;
using TheLostDungeon.Algorithms;

namespace TheLostDungeon.Online
{
    /// <summary>One row of the leaderboard (serializable so it can be cached and queued as JSON).</summary>
    [Serializable]
    public class LeaderboardEntry
    {
        public int rank;
        public string memberId;
        public string name;
        public int score;
        public int levels;              // levels completed
        public int stars;               // total stars
        public long submittedUtcTicks;  // tie-break: earlier submission ranks higher
        public bool isLocalPlayer;
        public bool pending;            // queued offline, not yet on the server

        /// <summary>Highest score first; equal scores keep the earlier submission first.</summary>
        public static int CompareForRanking(LeaderboardEntry a, LeaderboardEntry b)
        {
            int byScore = b.score.CompareTo(a.score);
            return byScore != 0 ? byScore : a.submittedUtcTicks.CompareTo(b.submittedUtcTicks);
        }

        /// <summary>Sorts with MERGE SORT (stable) and numbers the ranks 1..n.</summary>
        public static void Rank(List<LeaderboardEntry> entries)
        {
            MergeSort.Sort(entries, CompareForRanking);
            for (int i = 0; i < entries.Count; i++) entries[i].rank = i + 1;
        }
    }

    /// <summary>What the leaderboard is currently doing, for UI feedback.</summary>
    public enum LeaderboardStatus
    {
        Idle,
        Loading,
        Online,          // fresh data from the server
        Offline,         // server unreachable: showing cached / local scores
        NotConfigured    // no game key set: local-only leaderboard
    }

    /// <summary>The result of a request: success flag, data and a readable error.</summary>
    public struct ServiceResult<T>
    {
        public bool Success;
        public T Data;
        public string Error;

        public static ServiceResult<T> Ok(T data) => new ServiceResult<T> { Success = true, Data = data };
        public static ServiceResult<T> Fail(string error) => new ServiceResult<T> { Success = false, Error = error };
    }
}
