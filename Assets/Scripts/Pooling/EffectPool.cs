using System.Collections.Generic;
using UnityEngine;

namespace TheLostDungeon.Pooling
{
    /// <summary>
    /// Shared pools for one-off effects (explosions, dust) used by many objects. Each prefab gets
    /// its own PrefabPool the first time it's spawned, so 10 barrels share one explosion pool
    /// instead of each creating and destroying effects.
    /// </summary>
    public static class EffectPool
    {
        private static readonly Dictionary<GameObject, PrefabPool> pools = new Dictionary<GameObject, PrefabPool>();
        private static Transform container;

        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, int prewarm = 2)
        {
            if (prefab == null) return null;

            if (container == null)
            {
                container = new GameObject("[Pool] Effects").transform;
                pools.Clear();   // the old container (and its pooled objects) went with the last scene
            }

            if (!pools.TryGetValue(prefab, out PrefabPool pool))
            {
                pool = new PrefabPool(prefab, prewarm, container);
                pools[prefab] = pool;
            }
            return pool.Spawn(position, rotation);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            pools.Clear();
            container = null;
        }
    }
}
