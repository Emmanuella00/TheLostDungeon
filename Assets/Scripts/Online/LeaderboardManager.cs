using System;
using System.Collections;
using System.Collections.Generic;
using TheLostDungeon.Core;
using TheLostDungeon.Data;
using TheLostDungeon.Events;
using UnityEngine;

namespace TheLostDungeon.Online
{
    /// <summary>
    /// Online leaderboard for the whole campaign.
    ///
    /// When a level is completed (GameEvents.LevelCompleted) the player's CAMPAIGN SCORE (sum of
    /// their best score on every level, from the JSON save) is submitted through the REST API.
    /// The leaderboard screen calls Refresh() to download the top scores, which are ranked with
    /// merge sort (see LeaderboardEntry.Rank).
    ///
    /// Offline / failure handling:
    ///   - no game key configured  -> local-only leaderboard (still fully playable)
    ///   - request fails           -> score is queued and re-sent on the next successful connect;
    ///                                the last downloaded scores are shown from the cache
    ///   - every state is reported through Status / SubmitState so the UI can show feedback
    /// </summary>
    public class LeaderboardManager : Singleton<LeaderboardManager>
    {
        public enum SubmitState { None, Sending, Sent, Queued, LocalOnly }

        private const string NameKey = "leaderboard.playerName";

        private LeaderboardConfig config;
        private ILeaderboardService service;
        private readonly LocalLeaderboardStore store = new LocalLeaderboardStore();
        private Coroutine refreshRoutine;

        public List<LeaderboardEntry> Entries { get; private set; } = new List<LeaderboardEntry>();
        public LeaderboardStatus Status { get; private set; } = LeaderboardStatus.Idle;
        public SubmitState LastSubmit { get; private set; } = SubmitState.None;
        public string LastError { get; private set; }
        public bool IsOnlineConfigured => service != null && service.IsConfigured;

        /// <summary>Raised whenever entries, status or submit state change (the UI listens).</summary>
        public event Action Changed;

        public string PlayerName
        {
            get
            {
                string name = PlayerPrefs.GetString(NameKey, "");
                if (string.IsNullOrEmpty(name))
                {
                    name = "Explorer-" + UnityEngine.Random.Range(1000, 10000);
                    PlayerPrefs.SetString(NameKey, name);
                }
                return name;
            }
            set
            {
                string clean = (value ?? "").Trim();
                if (clean.Length == 0) return;
                if (clean.Length > 16) clean = clean.Substring(0, 16);
                PlayerPrefs.SetString(NameKey, clean);
                PlayerPrefs.Save();
                Changed?.Invoke();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Create()
        {
            if (HasInstance) return;
            new GameObject("LeaderboardManager").AddComponent<LeaderboardManager>();
        }

        protected override void OnSingletonAwake()
        {
            config = Resources.Load<LeaderboardConfig>("LeaderboardConfig");
            if (config == null) config = ScriptableObject.CreateInstance<LeaderboardConfig>();   // local-only
            service = new LootLockerLeaderboardService(config);
            Entries = store.LoadCache();
        }

        private void OnEnable() => GameEvents.LevelCompleted += HandleLevelCompleted;
        private void OnDisable() => GameEvents.LevelCompleted -= HandleLevelCompleted;

        // ------------------------------------------------------------------ public API

        /// <summary>Downloads the top scores (or falls back to local data). Safe to call repeatedly.</summary>
        public void Refresh()
        {
            if (refreshRoutine != null) return;
            refreshRoutine = StartCoroutine(RefreshRoutine());
        }

        /// <summary>The player's current campaign entry, built from the save file.</summary>
        public LeaderboardEntry BuildPlayerEntry()
        {
            int score = 0, levels = 0, stars = 0;
            if (SaveManager.HasInstance)
            {
                foreach (LevelProgressData level in SaveManager.Instance.Data.levels)
                {
                    if (!level.completed) continue;
                    score += level.bestScore;
                    stars += level.bestStars;
                    levels++;
                }
            }
            return new LeaderboardEntry
            {
                name = PlayerName,
                score = score,
                levels = levels,
                stars = stars,
                submittedUtcTicks = DateTime.UtcNow.Ticks,
                isLocalPlayer = true
            };
        }

        // ------------------------------------------------------------------ flow

        private void HandleLevelCompleted(LevelResult result)
        {
            LeaderboardEntry entry = BuildPlayerEntry();
            if (entry.score <= 0) return;
            StartCoroutine(SubmitRoutine(entry));
        }

        private IEnumerator SubmitRoutine(LeaderboardEntry entry)
        {
            if (!service.IsConfigured)
            {
                UpsertLocal(entry);
                SetSubmit(SubmitState.LocalOnly, null);
                yield break;
            }

            SetSubmit(SubmitState.Sending, null);
            ServiceResult<bool> result = default;
            yield return service.SubmitScore(entry, r => result = r);

            if (result.Success)
            {
                store.ClearPending();
                SetSubmit(SubmitState.Sent, null);
            }
            else
            {
                store.QueuePending(entry);   // retried on the next Refresh
                SetSubmit(SubmitState.Queued, result.Error);
            }
        }

        private IEnumerator RefreshRoutine()
        {
            SetStatus(LeaderboardStatus.Loading, null);

            if (!service.IsConfigured)
            {
                UpsertLocal(BuildPlayerEntry());
                SetStatus(LeaderboardStatus.NotConfigured, null);
                refreshRoutine = null;
                yield break;
            }

            // 1. Re-send a score that was queued while offline.
            LeaderboardEntry pending = store.Pending;
            if (pending != null)
            {
                ServiceResult<bool> sent = default;
                yield return service.SubmitScore(pending, r => sent = r);
                if (sent.Success) store.ClearPending();
            }

            // 2. Download the top scores.
            ServiceResult<List<LeaderboardEntry>> top = default;
            yield return service.GetTopScores(config.entriesToShow, r => top = r);

            if (top.Success)
            {
                store.SaveCache(top.Data);
                Entries = WithPending(top.Data);
                SetStatus(LeaderboardStatus.Online, null);
            }
            else
            {
                Entries = WithPending(store.LoadCache());
                SetStatus(LeaderboardStatus.Offline, top.Error);
            }
            refreshRoutine = null;
        }

        /// <summary>Adds the not-yet-sent score (if any) to the list and re-ranks with merge sort.</summary>
        private List<LeaderboardEntry> WithPending(List<LeaderboardEntry> entries)
        {
            var list = new List<LeaderboardEntry>(entries);
            LeaderboardEntry pending = store.Pending;
            if (pending != null)
            {
                list.RemoveAll(e => e.isLocalPlayer && e.score <= pending.score);
                pending.isLocalPlayer = true;
                list.Add(pending);
            }
            LeaderboardEntry.Rank(list);
            return list;
        }

        /// <summary>Local-only mode: keeps the player's own best entry in the cached list.</summary>
        private void UpsertLocal(LeaderboardEntry entry)
        {
            List<LeaderboardEntry> list = store.LoadCache();
            LeaderboardEntry existing = list.Find(e => e.isLocalPlayer);
            if (existing == null || entry.score > existing.score)
            {
                list.RemoveAll(e => e.isLocalPlayer);
                if (entry.score > 0) list.Add(entry);
            }
            else
            {
                existing.name = entry.name;
            }
            LeaderboardEntry.Rank(list);
            store.SaveCache(list);
            Entries = list;
        }

        private void SetStatus(LeaderboardStatus status, string error)
        {
            Status = status;
            LastError = error;
            Changed?.Invoke();
        }

        private void SetSubmit(SubmitState state, string error)
        {
            LastSubmit = state;
            if (error != null) LastError = error;
            Changed?.Invoke();
        }
    }
}
