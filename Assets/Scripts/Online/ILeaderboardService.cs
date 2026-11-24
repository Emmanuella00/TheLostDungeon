using System;
using System.Collections;
using System.Collections.Generic;

namespace TheLostDungeon.Online
{
    /// <summary>
    /// Abstraction over the online leaderboard (DEPENDENCY INVERSION / STRATEGY): the game talks
    /// to this interface only, so the REST provider (LootLocker today) could be swapped for
    /// another backend, or a fake one in tests, without touching the UI or game code.
    ///
    /// Methods are coroutines (IEnumerator) so they work on every platform, including WebGL,
    /// where threads and blocking calls aren't available.
    /// </summary>
    public interface ILeaderboardService
    {
        bool IsConfigured { get; }

        /// <summary>Logs in (guest session) if needed.</summary>
        IEnumerator Connect(Action<ServiceResult<bool>> done);

        IEnumerator SubmitScore(LeaderboardEntry entry, Action<ServiceResult<bool>> done);

        IEnumerator GetTopScores(int count, Action<ServiceResult<List<LeaderboardEntry>>> done);
    }
}
