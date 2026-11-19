using System;
using UnityEngine;

namespace TheLostDungeon.Data
{
    /// <summary>
    /// Converts SaveData to and from JSON. Pure logic with no file access, so it can be unit
    /// tested. A corrupted or empty file never crashes the game: it returns null and the
    /// caller starts a fresh save instead.
    /// </summary>
    public static class SaveSerializer
    {
        public static string ToJson(SaveData data, bool prettyPrint = true)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            return JsonUtility.ToJson(data, prettyPrint);
        }

        public static SaveData FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            try
            {
                SaveData data = JsonUtility.FromJson<SaveData>(json);
                if (data == null) return null;

                // Guard against fields missing from older or hand-edited files.
                data.levels ??= new System.Collections.Generic.List<LevelProgressData>();
                data.stats ??= new PlayerStatsData();
                data.inventory ??= new System.Collections.Generic.List<InventoryItemData>();
                data.unlockedAbilities ??= new System.Collections.Generic.List<string>();
                return data;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSerializer] Could not read save data, starting fresh. {e.Message}");
                return null;
            }
        }
    }
}
