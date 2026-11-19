using TheLostDungeon.Events;
using UnityEngine;

namespace TheLostDungeon.Core
{
    /// <summary>
    /// Pure observer: listens to gameplay events and updates the saved player statistics.
    /// Nothing calls this class directly. That's the point: enemies, pickups and the player
    /// don't know stats exist, yet every kill, death and credit is counted.
    /// </summary>
    public class PlayerStatsTracker : MonoBehaviour
    {
        private void OnEnable()
        {
            GameEvents.EnemyDefeated += HandleEnemyDefeated;
            GameEvents.PlayerDied += HandlePlayerDied;
            GameEvents.ItemCollected += HandleItemCollected;
        }

        private void OnDisable()
        {
            GameEvents.EnemyDefeated -= HandleEnemyDefeated;
            GameEvents.PlayerDied -= HandlePlayerDied;
            GameEvents.ItemCollected -= HandleItemCollected;
        }

        private void Update()
        {
            if (GameManager.HasInstance && GameManager.Instance.IsPlaying && SaveManager.HasInstance)
            {
                SaveManager.Instance.Data.stats.totalPlayTimeSeconds += Time.unscaledDeltaTime;
            }
        }

        private void HandleEnemyDefeated(EnemyDefeatedInfo info)
        {
            if (SaveManager.HasInstance) SaveManager.Instance.Data.stats.enemiesDefeated++;
        }

        private void HandlePlayerDied()
        {
            if (!SaveManager.HasInstance) return;
            SaveManager.Instance.Data.stats.deaths++;
            SaveManager.Instance.Save();
        }

        private void HandleItemCollected(ItemCollectedInfo info)
        {
            if (SaveManager.HasInstance && info.ItemId == "credits")
            {
                SaveManager.Instance.Data.stats.credits += info.Amount;
            }
        }
    }
}
