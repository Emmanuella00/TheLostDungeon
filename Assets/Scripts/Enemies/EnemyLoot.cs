using TheLostDungeon.Combat;
using TheLostDungeon.Items;
using UnityEngine;

namespace TheLostDungeon.Enemies
{
    /// <summary>
    /// Drops pickups when the enemy dies:
    ///   - guaranteed drops: always (e.g. the Guardian that carries the red keycard)
    ///   - a random roll from a LootTable (weighted random) with a drop chance
    /// Reuses the same LootTable assets as chests.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class EnemyLoot : MonoBehaviour
    {
        [SerializeField] private GameObject[] guaranteedDrops = new GameObject[0];
        [SerializeField] private LootTable lootTable;
        [SerializeField, Range(0f, 1f)] private float dropChance = 0.5f;

        private Health health;

        private void Awake() => health = GetComponent<Health>();
        private void OnEnable() => health.Died += Drop;
        private void OnDisable() => health.Died -= Drop;

        private void Drop()
        {
            Vector3 origin = transform.position + Vector3.up * 0.2f;
            int index = 0;

            foreach (GameObject prefab in guaranteedDrops)
            {
                if (prefab != null) Instantiate(prefab, origin + Offset(index++), Quaternion.identity);
            }

            if (lootTable != null && Random.value <= dropChance)
            {
                GameObject rolled = lootTable.Roll(Random.value);
                if (rolled != null) Instantiate(rolled, origin + Offset(index), Quaternion.identity);
            }
        }

        private static Vector3 Offset(int i)
        {
            if (i == 0) return Vector3.zero;
            float angle = i * 2.1f;
            return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 0.7f;
        }
    }
}
