using System.Collections.Generic;
using TheLostDungeon.Core;
using TheLostDungeon.Data;
using TheLostDungeon.Events;
using UnityEngine;

namespace TheLostDungeon.Items
{
    /// <summary>
    /// The player's inventory. Implements ISaveable: SaveManager asks it to write itself into the
    /// JSON save, and only PERSISTENT items (credits) are saved. Level items like keycards are
    /// lost when the level ends, which is what you want for a keycard puzzle.
    /// </summary>
    public class PlayerInventory : MonoBehaviour, ISaveable
    {
        private readonly Inventory inventory = new Inventory();
        private readonly Dictionary<string, ItemDefinition> definitions = new Dictionary<string, ItemDefinition>();

        [Tooltip("Persistent items the save may contain (e.g. Credits), so they can be recognised on load.")]
        [SerializeField] private ItemDefinition[] persistentItems = new ItemDefinition[0];

        public IReadOnlyDictionary<string, int> Items => inventory.Items;

        private void Awake()
        {
            foreach (ItemDefinition def in persistentItems)
            {
                if (def != null) definitions[def.Id] = def;
            }
            inventory.Changed += (id, count) => GameEvents.RaiseInventoryChanged();
        }

        private void Start()
        {
            if (SaveManager.HasInstance) SaveManager.Instance.Register(this);   // restores saved credits
        }

        private void OnDestroy()
        {
            if (SaveManager.HasInstance) SaveManager.Instance.Unregister(this);
        }

        public int Count(ItemDefinition item) => item != null ? inventory.Count(item.Id) : 0;
        public int Count(string itemId) => inventory.Count(itemId);
        public bool Has(ItemDefinition item, int amount = 1) => item != null && inventory.Has(item.Id, amount);

        /// <summary>Adds items and announces the pickup. Returns how many were added.</summary>
        public int Add(ItemDefinition item, int amount)
        {
            if (item == null) return 0;
            definitions[item.Id] = item;

            int added = inventory.Add(item.Id, amount, item.MaxStack);
            if (added > 0) GameEvents.RaiseItemCollected(new ItemCollectedInfo(item.Id, added));
            return added;
        }

        public bool Remove(ItemDefinition item, int amount = 1) => item != null && inventory.Remove(item.Id, amount);

        public string DisplayNameOf(string itemId) =>
            definitions.TryGetValue(itemId, out ItemDefinition def) ? def.DisplayName : itemId;

        // ----- ISaveable -----

        public void CaptureState(SaveData data)
        {
            data.inventory = inventory.ToSaveData(id => definitions.TryGetValue(id, out ItemDefinition def) && def.Persistent);
        }

        public void RestoreState(SaveData data)
        {
            inventory.LoadFrom(data.inventory);
            GameEvents.RaiseInventoryChanged();
        }
    }
}
