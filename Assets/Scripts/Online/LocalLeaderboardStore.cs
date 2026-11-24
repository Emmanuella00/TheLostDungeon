using System;
using System.Collections.Generic;
using UnityEngine;

namespace TheLostDungeon.Online
{
    /// <summary>
    /// OFFLINE SUPPORT for the leaderboard, stored as JSON in PlayerPrefs (works on WebGL too):
    ///   - cache:   the last top scores downloaded, shown when the server can't be reached
    ///   - pending: the player's newest score that couldn't be sent yet; it is retried the next
    ///              time the game connects (only the best one is kept, since the server only
    ///              keeps each player's best score anyway)
    /// </summary>
    public class LocalLeaderboardStore
    {
        private const string CacheKey = "leaderboard.cache";
        private const string PendingKey = "leaderboard.pending";

        [Serializable]
        private class EntryList
        {
            public List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
        }

        public List<LeaderboardEntry> LoadCache() => Load(CacheKey).entries;

        public void SaveCache(List<LeaderboardEntry> entries) => Save(CacheKey, new EntryList { entries = entries });

        public LeaderboardEntry Pending
        {
            get
            {
                List<LeaderboardEntry> pending = Load(PendingKey).entries;
                return pending.Count > 0 ? pending[0] : null;
            }
        }

        public void QueuePending(LeaderboardEntry entry)
        {
            LeaderboardEntry current = Pending;
            if (current != null && current.score >= entry.score) return;   // keep the best one
            entry.pending = true;
            Save(PendingKey, new EntryList { entries = new List<LeaderboardEntry> { entry } });
        }

        public void ClearPending()
        {
            PlayerPrefs.DeleteKey(PendingKey);
            PlayerPrefs.Save();
        }

        private static EntryList Load(string key)
        {
            string json = PlayerPrefs.GetString(key, "");
            if (string.IsNullOrEmpty(json)) return new EntryList();
            try
            {
                EntryList list = JsonUtility.FromJson<EntryList>(json);
                return list != null && list.entries != null ? list : new EntryList();
            }
            catch (Exception)
            {
                return new EntryList();   // corrupted cache: start fresh rather than crash
            }
        }

        private static void Save(string key, EntryList list)
        {
            PlayerPrefs.SetString(key, JsonUtility.ToJson(list));
            PlayerPrefs.Save();
        }
    }
}
