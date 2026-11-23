using UnityEngine;

namespace TheLostDungeon.Pooling
{
    /// <summary>
    /// Added to every pooled instance. Call ReturnToPool() instead of Destroy().
    /// Optionally returns itself automatically after a lifetime (used by impact effects).
    /// </summary>
    public class PooledObject : MonoBehaviour
    {
        [Tooltip("If > 0, the object returns to its pool automatically after this many seconds.")]
        [SerializeField, Min(0f)] private float autoReturnSeconds = 0f;

        private IPoolable[] poolables;
        // Starts at "never": objects placed in a scene by hand (not spawned from a pool) must not
        // time out on their first frame. NotifySpawned sets the real time for pooled spawns.
        private float returnAt = float.PositiveInfinity;

        public PrefabPool Owner { get; set; }

        private void Awake() => poolables = GetComponentsInChildren<IPoolable>(true);

        internal void NotifySpawned()
        {
            returnAt = autoReturnSeconds > 0f ? Time.time + autoReturnSeconds : float.PositiveInfinity;
            if (poolables == null) poolables = GetComponentsInChildren<IPoolable>(true);
            foreach (IPoolable p in poolables) p.OnSpawn();
        }

        internal void NotifyDespawned()
        {
            if (poolables == null) return;
            foreach (IPoolable p in poolables) p.OnDespawn();
        }

        private void Update()
        {
            if (Time.time >= returnAt) ReturnToPool();
        }

        public void ReturnToPool()
        {
            returnAt = float.PositiveInfinity;
            if (Owner != null && Owner.Despawn(this)) return;
            if (Owner == null) Destroy(gameObject);   // not pooled (e.g. placed by hand)
        }
    }
}