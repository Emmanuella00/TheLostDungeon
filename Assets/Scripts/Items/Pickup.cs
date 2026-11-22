using TheLostDungeon.Combat;
using TheLostDungeon.Events;
using UnityEngine;

namespace TheLostDungeon.Items
{
    /// <summary>
    /// Walk-over pickup (ICollectable). One component covers both kinds:
    ///   Item   - adds an ItemDefinition to the inventory (keycards, credits)
    ///   Health - heals the player; ignored at full health so it isn't wasted
    /// Needs a trigger collider. The visual child spins and bobs so pickups are easy to spot.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Pickup : MonoBehaviour, ICollectable
    {
        public enum PickupKind { Item, Health }

        [SerializeField] private PickupKind kind = PickupKind.Item;
        [SerializeField] private ItemDefinition item;
        [SerializeField, Min(1)] private int amount = 1;
        [SerializeField, Min(1f)] private float healAmount = 35f;

        [Header("Visual")]
        [SerializeField] private Transform visual;
        [SerializeField] private float spinDegreesPerSecond = 90f;
        [SerializeField] private float bobHeight = 0.08f;
        [SerializeField] private float bobSpeed = 2f;

        private Vector3 visualStart;
        private bool collected;

        public PickupKind Kind => kind;
        public ItemDefinition Item => item;

        private void Reset() => GetComponent<Collider>().isTrigger = true;

        private void Awake()
        {
            if (visual != null) visualStart = visual.localPosition;
        }

        private void Update()
        {
            if (visual == null) return;
            visual.Rotate(Vector3.up, spinDegreesPerSecond * Time.deltaTime, Space.World);
            visual.localPosition = visualStart + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerInventory inventory = other.GetComponentInParent<PlayerInventory>();
            if (inventory != null) TryCollect(inventory.gameObject);
        }

        public bool TryCollect(GameObject collector)
        {
            if (collected || collector == null) return false;

            bool taken = kind == PickupKind.Health ? TryHeal(collector) : TryAddItem(collector);
            if (!taken) return false;

            collected = true;
            gameObject.SetActive(false);
            return true;
        }

        private bool TryHeal(GameObject collector)
        {
            Health health = collector.GetComponent<Health>();
            if (health == null || !health.IsAlive || health.Current >= health.Max) return false;

            health.Heal(healAmount);
            GameEvents.RaiseItemCollected(new ItemCollectedInfo("health_pack", 1));
            return true;
        }

        private bool TryAddItem(GameObject collector)
        {
            PlayerInventory inventory = collector.GetComponent<PlayerInventory>();
            return inventory != null && inventory.Add(item, amount) > 0;
        }
    }
}
