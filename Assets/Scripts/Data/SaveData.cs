using System;
using System.Collections.Generic;

namespace TheLostDungeon.Data
{
    /// <summary>
    /// Everything written to the JSON save file. Plain [Serializable] classes so Unity's
    /// JsonUtility can convert them. JsonUtility does not support Dictionary, so lists are used.
    ///
    /// Settings are NOT stored here; they live in PlayerPrefs (see GameSettings), because they
    /// are small device preferences rather than game progress.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public string lastSavedUtc = string.Empty;

        public List<LevelProgressData> levels = new List<LevelProgressData>();
        public PlayerStatsData stats = new PlayerStatsData();
        public List<InventoryItemData> inventory = new List<InventoryItemData>();
        public List<string> unlockedAbilities = new List<string>();
    }

    [Serializable]
    public class LevelProgressData
    {
        public int levelIndex;
        public bool completed;
        public float bestTimeSeconds;   // 0 = no time recorded yet
        public int bestStars;
        public int bestScore;
    }

    [Serializable]
    public class PlayerStatsData
    {
        public int enemiesDefeated;
        public int deaths;
        public int credits;
        public float totalPlayTimeSeconds;
    }

    [Serializable]
    public class InventoryItemData
    {
        public string itemId;
        public int amount;
    }
}
