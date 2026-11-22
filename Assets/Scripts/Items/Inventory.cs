using System;
using System.Collections.Generic;
using TheLostDungeon.Data;

namespace TheLostDungeon.Items
{
    /// <summary>
    /// Item counts by id, as plain C# so it's unit testable. Enforces stack limits and never
    /// lets a count go negative. PlayerInventory wraps it for the game.
    /// </summary>
    public class Inventory
    {
        private readonly Dictionary<string, int> counts = new Dictionary<string, int>();

        /// <summary>(itemId, newCount) whenever a count changes.</summary>
        public event Action<string, int> Changed;

        public IReadOnlyDictionary<string, int> Items => counts;

        public int Count(string itemId) => itemId != null && counts.TryGetValue(itemId, out int c) ? c : 0;

        public bool Has(string itemId, int amount = 1) => Count(itemId) >= amount;

        /// <summary>Adds up to the stack limit. Returns how many were actually added.</summary>
        public int Add(string itemId, int amount, int maxStack = int.MaxValue)
        {
            if (string.IsNullOrEmpty(itemId) || amount <= 0) return 0;

            int current = Count(itemId);
            int space = Math.Max(0, maxStack - current);
            int added = Math.Min(space, amount);
            if (added == 0) return 0;

            counts[itemId] = current + added;
            Changed?.Invoke(itemId, current + added);
            return added;
        }

        /// <summary>Removes the amount only if there is enough. Returns false otherwise.</summary>
        public bool Remove(string itemId, int amount = 1)
        {
            if (amount <= 0 || !Has(itemId, amount)) return false;

            int remaining = Count(itemId) - amount;
            if (remaining == 0) counts.Remove(itemId);
            else counts[itemId] = remaining;

            Changed?.Invoke(itemId, remaining);
            return true;
        }

        public void Clear()
        {
            var ids = new List<string>(counts.Keys);
            counts.Clear();
            foreach (string id in ids) Changed?.Invoke(id, 0);
        }

        /// <summary>Converts to save data, keeping only items the filter accepts (e.g. persistent ones).</summary>
        public List<InventoryItemData> ToSaveData(Func<string, bool> include)
        {
            var list = new List<InventoryItemData>();
            foreach (KeyValuePair<string, int> pair in counts)
            {
                if (include == null || include(pair.Key))
                    list.Add(new InventoryItemData { itemId = pair.Key, amount = pair.Value });
            }
            return list;
        }

        public void LoadFrom(IEnumerable<InventoryItemData> items)
        {
            counts.Clear();
            if (items == null) return;
            foreach (InventoryItemData item in items)
            {
                if (item != null && !string.IsNullOrEmpty(item.itemId) && item.amount > 0)
                    counts[item.itemId] = Count(item.itemId) + item.amount;
            }
        }
    }
}
