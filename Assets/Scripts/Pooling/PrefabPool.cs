using UnityEngine;

namespace TheLostDungeon.Pooling
{
    /// <summary>
    /// Unity-side wrapper around ObjectPool for prefabs (bolts, impact sparks, muzzle flashes).
    /// Spawned objects get a PooledObject component so they know which pool to return to.
    /// </summary>
    public class PrefabPool
    {
        private readonly GameObject prefab;
        private readonly Transform container;
        private readonly ObjectPool<PooledObject> pool;

        public int CountActive => pool.CountActive;
        public int CountInactive => pool.CountInactive;
        public GameObject Prefab => prefab;

        public PrefabPool(GameObject prefab, int prewarm, Transform container = null)
        {
            this.prefab = prefab;
            this.container = container;
            pool = new ObjectPool<PooledObject>(Create, OnGet, OnRelease, prewarm);
        }

        public GameObject Spawn(Vector3 position, Quaternion rotation)
        {
            PooledObject pooled = pool.Get();
            if (pooled == null) return null;

            Transform t = pooled.transform;
            t.SetPositionAndRotation(position, rotation);
            pooled.gameObject.SetActive(true);
            pooled.NotifySpawned();
            return pooled.gameObject;
        }

        public bool Despawn(PooledObject pooled) => pooled != null && pool.Release(pooled);

        /// <summary>Destroys all inactive instances (call when the owner is destroyed).</summary>
        public void Clear() => pool.Clear(p => { if (p != null) Object.Destroy(p.gameObject); });

        private PooledObject Create()
        {
            GameObject instance = Object.Instantiate(prefab, container);
            instance.name = prefab.name;
            PooledObject pooled = instance.GetComponent<PooledObject>();
            if (pooled == null) pooled = instance.AddComponent<PooledObject>();
            pooled.Owner = this;
            return pooled;
        }

        private static void OnGet(PooledObject pooled) { }

        private static void OnRelease(PooledObject pooled)
        {
            if (pooled == null) return;
            pooled.NotifyDespawned();
            pooled.gameObject.SetActive(false);
        }
    }
}
