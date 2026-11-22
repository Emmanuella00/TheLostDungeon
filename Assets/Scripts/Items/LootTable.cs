using System;
using System.Collections.Generic;
using TheLostDungeon.Algorithms;
using UnityEngine;

namespace TheLostDungeon.Items
{
    /// <summary>
    /// What a chest (or later, an enemy) can drop, with a weight per entry.
    /// Uses the WeightedRandom algorithm. Create > The Lost Dungeon > Loot Table.
    /// </summary>
    [CreateAssetMenu(menuName = "The Lost Dungeon/Loot Table", fileName = "LT_")]
    public class LootTable : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public GameObject pickupPrefab;
            [Min(0f)] public float weight = 1f;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        private readonly List<float> weightBuffer = new List<float>();

        public IReadOnlyList<Entry> Entries => entries;

        /// <summary>Picks one pickup prefab using the weights, or null if the table is empty.</summary>
        public GameObject Roll(float random01)
        {
            weightBuffer.Clear();
            foreach (Entry e in entries) weightBuffer.Add(e != null && e.pickupPrefab != null ? e.weight : 0f);

            int index = WeightedRandom.PickIndex(weightBuffer, random01);
            return index >= 0 ? entries[index].pickupPrefab : null;
        }

#if UNITY_EDITOR
        public void EditorSetEntries(List<Entry> newEntries) => entries = newEntries;
#endif
    }
}
