using System;
using TheLostDungeon.Data;

namespace TheLostDungeon.Levels
{
    public enum LevelStatus
    {
        Locked,
        Available,
        Completed
    }

    /// <summary>
    /// The unlock rules, as plain C# (no MonoBehaviour) so they are easy to unit test:
    ///   - Level 1 is always available.
    ///   - Any other level is available once the level before it is completed.
    ///   - Locked levels can't be completed or loaded.
    /// It reads and writes the LevelProgressData list inside SaveData.
    /// </summary>
    public class LevelProgression
    {
        private readonly SaveData data;

        public int LevelCount { get; }

        public LevelProgression(SaveData data, int levelCount)
        {
            if (levelCount <= 0) throw new ArgumentOutOfRangeException(nameof(levelCount));
            this.data = data ?? throw new ArgumentNullException(nameof(data));
            LevelCount = levelCount;
            EnsureEntries();
        }

        public LevelStatus GetStatus(int levelIndex)
        {
            ValidateIndex(levelIndex);

            if (Entry(levelIndex).completed) return LevelStatus.Completed;
            if (levelIndex == 0 || Entry(levelIndex - 1).completed) return LevelStatus.Available;
            return LevelStatus.Locked;
        }

        public bool IsUnlocked(int levelIndex) => GetStatus(levelIndex) != LevelStatus.Locked;

        public bool HasAnyProgress => data.levels.Exists(l => l.completed);

        /// <summary>First level that is available but not yet completed (or the last level).</summary>
        public int NextLevelToPlay
        {
            get
            {
                for (int i = 0; i < LevelCount; i++)
                {
                    if (GetStatus(i) == LevelStatus.Available) return i;
                }
                return LevelCount - 1;
            }
        }

        /// <summary>
        /// Records a completion. Returns false (and changes nothing) if the level is locked.
        /// Keeps the best (lowest) time and the highest stars and score.
        /// </summary>
        public bool CompleteLevel(int levelIndex, float timeSeconds, int stars, int score, out bool unlockedNewLevel)
        {
            unlockedNewLevel = false;
            if (!IsUnlocked(levelIndex)) return false;

            bool nextWasLocked = levelIndex + 1 < LevelCount && GetStatus(levelIndex + 1) == LevelStatus.Locked;

            LevelProgressData entry = Entry(levelIndex);
            entry.completed = true;
            if (entry.bestTimeSeconds <= 0f || timeSeconds < entry.bestTimeSeconds) entry.bestTimeSeconds = timeSeconds;
            entry.bestStars = Math.Max(entry.bestStars, stars);
            entry.bestScore = Math.Max(entry.bestScore, score);

            unlockedNewLevel = nextWasLocked;
            return true;
        }

        public LevelProgressData GetRecord(int levelIndex)
        {
            ValidateIndex(levelIndex);
            return Entry(levelIndex);
        }

        /// <summary>3 stars at or under par time, 2 stars under 1.5x par, otherwise 1.</summary>
        public static int CalculateStars(float timeSeconds, float parTimeSeconds)
        {
            if (parTimeSeconds <= 0f) return 1;
            if (timeSeconds <= parTimeSeconds) return 3;
            if (timeSeconds <= parTimeSeconds * 1.5f) return 2;
            return 1;
        }

        private LevelProgressData Entry(int levelIndex) => data.levels.Find(l => l.levelIndex == levelIndex);

        private void EnsureEntries()
        {
            for (int i = 0; i < LevelCount; i++)
            {
                if (Entry(i) == null) data.levels.Add(new LevelProgressData { levelIndex = i });
            }
        }

        private void ValidateIndex(int levelIndex)
        {
            if (levelIndex < 0 || levelIndex >= LevelCount)
                throw new ArgumentOutOfRangeException(nameof(levelIndex), $"Level {levelIndex} doesn't exist (0..{LevelCount - 1}).");
        }
    }
}
