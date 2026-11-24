using TheLostDungeon.Enemies;
using TheLostDungeon.Events;
using UnityEngine;

namespace TheLostDungeon.Objectives
{
    /// <summary>
    /// "Destroy N enemies". Counts GameEvents.EnemyDefeated while active, optionally one enemy type.
    /// With "Defeat All In Level" ticked, N is worked out when the objective starts: every living
    /// enemy in the scene plus everything the spawners will still spawn.
    /// </summary>
    public class DefeatEnemiesObjective : Objective
    {
        [SerializeField, Min(1)] private int count = 5;
        [Tooltip("Leave empty to count any enemy, or e.g. 'warrior_bot' for one type.")]
        [SerializeField] private string enemyIdFilter = "";
        [Tooltip("Ignore Count and require every enemy in the level (placed + still to spawn).")]
        [SerializeField] private bool defeatAllInLevel;

        private int defeated;

        public override int Current => Mathf.Min(defeated, count);
        public override int Target => count;

        protected override void OnBegin()
        {
            if (defeatAllInLevel) count = Mathf.Max(1, CountEnemiesInLevel());
            GameEvents.EnemyDefeated += HandleEnemyDefeated;
        }

        private static int CountEnemiesInLevel()
        {
            int total = 0;
            foreach (EnemyBrain enemy in FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))
            {
                if (enemy.IsAlive) total++;
            }
            foreach (EnemySpawner spawner in FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None))
            {
                total += Mathf.Max(0, spawner.TotalToSpawn - spawner.Spawned);
            }
            return total;
        }
        protected override void OnEnd() => GameEvents.EnemyDefeated -= HandleEnemyDefeated;

        private void HandleEnemyDefeated(EnemyDefeatedInfo info)
        {
            if (!string.IsNullOrEmpty(enemyIdFilter) && info.EnemyId != enemyIdFilter) return;
            defeated++;
            NotifyProgress();
        }
    }
}
