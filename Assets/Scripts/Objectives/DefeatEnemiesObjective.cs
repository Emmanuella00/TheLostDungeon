using TheLostDungeon.Events;
using UnityEngine;

namespace TheLostDungeon.Objectives
{
    /// <summary>"Destroy N enemies". Counts GameEvents.EnemyDefeated while active, optionally one enemy type.</summary>
    public class DefeatEnemiesObjective : Objective
    {
        [SerializeField, Min(1)] private int count = 5;
        [Tooltip("Leave empty to count any enemy, or e.g. 'warrior_bot' for one type.")]
        [SerializeField] private string enemyIdFilter = "";

        private int defeated;

        public override int Current => Mathf.Min(defeated, count);
        public override int Target => count;

        protected override void OnBegin() => GameEvents.EnemyDefeated += HandleEnemyDefeated;
        protected override void OnEnd() => GameEvents.EnemyDefeated -= HandleEnemyDefeated;

        private void HandleEnemyDefeated(EnemyDefeatedInfo info)
        {
            if (!string.IsNullOrEmpty(enemyIdFilter) && info.EnemyId != enemyIdFilter) return;
            defeated++;
            NotifyProgress();
        }
    }
}
