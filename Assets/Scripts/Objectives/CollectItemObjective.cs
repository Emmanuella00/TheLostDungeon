using TheLostDungeon.Events;
using TheLostDungeon.Items;
using UnityEngine;

namespace TheLostDungeon.Objectives
{
    /// <summary>"Find the red keycard". Counts GameEvents.ItemCollected for one item.</summary>
    public class CollectItemObjective : Objective
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField, Min(1)] private int count = 1;

        private int collected;

        public override int Current => Mathf.Min(collected, count);
        public override int Target => count;

        protected override void OnBegin() => GameEvents.ItemCollected += HandleItemCollected;
        protected override void OnEnd() => GameEvents.ItemCollected -= HandleItemCollected;

        private void HandleItemCollected(ItemCollectedInfo info)
        {
            if (item == null || info.ItemId != item.Id) return;
            collected += info.Amount;
            NotifyProgress();
        }
    }
}
