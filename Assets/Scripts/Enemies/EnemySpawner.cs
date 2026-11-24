using System.Collections;
using System.Collections.Generic;
using TheLostDungeon.Interaction;
using TheLostDungeon.Player;
using TheLostDungeon.Pooling;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace TheLostDungeon.Enemies
{
    /// <summary>
    /// Spawns waves of enemies from OBJECT POOLS: dead enemies go back to the pool and are reused
    /// for the next spawn instead of being destroyed and re-created.
    ///
    /// It implements IActivatable, so the EXISTING WallSwitch (or an ActivationZone trigger) can
    /// start a wave with no new code: "pull the lockdown lever, enemies pour in".
    ///
    /// ADDRESSABLES: enemy prefabs are AssetReferences, loaded when the level starts and released
    /// when the spawner is destroyed (level exit). The pools hold the loaded prefabs, so the assets
    /// stay available for as long as enemies can still be spawned from them.
    /// </summary>
    public class EnemySpawner : MonoBehaviour, IActivatable
    {
        [Tooltip("Addressable enemy prefabs to spawn, cycled in order (e.g. Warrior, Warrior, Archer).")]
        [SerializeField] private AssetReferenceGameObject[] enemies = new AssetReferenceGameObject[0];
        [Tooltip("Where they appear, cycled in order. Empty = this object's position.")]
        [SerializeField] private Transform[] spawnPoints = new Transform[0];

        [SerializeField, Min(1)] private int totalToSpawn = 6;
        [SerializeField, Min(1)] private int maxAliveAtOnce = 3;
        [SerializeField, Min(0.1f)] private float secondsBetweenSpawns = 2f;
        [SerializeField] private bool startOnPlay;
        [Tooltip("New enemies head straight for the player instead of patrolling.")]
        [SerializeField] private bool alertOnSpawn = true;
        [SerializeField, Min(0)] private int prewarmPerPrefab = 2;

        private readonly Dictionary<string, PrefabPool> poolsByKey = new Dictionary<string, PrefabPool>();
        private readonly List<AsyncOperationHandle<GameObject>> handles = new List<AsyncOperationHandle<GameObject>>();
        private readonly List<EnemyBrain> alive = new List<EnemyBrain>();
        private bool loaded;
        private bool activateWhenLoaded;
        private Transform container;
        private Coroutine routine;
        private int spawned;

        public int Spawned => spawned;
        public int TotalToSpawn => totalToSpawn;
        public bool IsRunning => routine != null;
        public bool Finished => spawned >= totalToSpawn && CountAlive() == 0;
        public bool IsLoaded => loaded;

        private void Awake()
        {
            container = new GameObject($"[Pool] {name}").transform;
        }

        private IEnumerator Start()
        {
            yield return LoadEnemyPrefabs();
            if (startOnPlay || activateWhenLoaded) SetActivated(true);
        }

        /// <summary>Loads each distinct enemy prefab once (Addressables reference counting) and builds its pool.</summary>
        private IEnumerator LoadEnemyPrefabs()
        {
            foreach (AssetReferenceGameObject reference in enemies)
            {
                if (reference == null || !reference.RuntimeKeyIsValid()) continue;
                string key = reference.RuntimeKey.ToString();
                if (poolsByKey.ContainsKey(key)) continue;

                AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(reference.RuntimeKey);
                handles.Add(handle);
                yield return handle;

                if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
                {
                    poolsByKey[key] = new PrefabPool(handle.Result, prewarmPerPrefab, container);
                }
                else
                {
                    // Graceful failure: this enemy type is skipped, the rest of the level still works.
                    Debug.LogWarning($"[EnemySpawner] {name}: couldn't load enemy '{key}'. {handle.OperationException?.Message}");
                }
            }
            loaded = true;
        }

        private void OnDestroy()
        {
            // Destroy pooled instances first, then release the prefabs they were made from.
            foreach (PrefabPool pool in poolsByKey.Values) pool.Clear();
            if (container != null) Destroy(container.gameObject);
            foreach (AsyncOperationHandle<GameObject> handle in handles)
            {
                if (handle.IsValid()) Addressables.Release(handle);
            }
            handles.Clear();
        }

        public void SetActivated(bool activated)
        {
            if (activated && !loaded)
            {
                activateWhenLoaded = true;   // e.g. switch pulled during the first frames: start once loaded
                return;
            }

            if (activated && routine == null && spawned < totalToSpawn) routine = StartCoroutine(SpawnLoop());
            else if (!activated && routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }
        }

        private IEnumerator SpawnLoop()
        {
            var wait = new WaitForSeconds(secondsBetweenSpawns);
            while (spawned < totalToSpawn)
            {
                if (CountAlive() < maxAliveAtOnce) SpawnOne();
                yield return wait;
            }
            routine = null;
        }

        private void SpawnOne()
        {
            if (enemies.Length == 0) return;

            AssetReferenceGameObject reference = enemies[spawned % enemies.Length];
            Transform point = spawnPoints.Length > 0 ? spawnPoints[spawned % spawnPoints.Length] : transform;
            spawned++;
            if (reference == null || point == null || !reference.RuntimeKeyIsValid()) return;
            if (!poolsByKey.TryGetValue(reference.RuntimeKey.ToString(), out PrefabPool pool)) return;   // failed to load

            Vector3 position = point.position;
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, 3f, NavMesh.AllAreas)) position = hit.position;

            GameObject enemy = pool.Spawn(position, point.rotation);
            if (enemy == null || !enemy.TryGetComponent(out EnemyBrain brain)) return;

            alive.Add(brain);
            if (alertOnSpawn)
            {
                PlayerController player = FindAnyObjectByType<PlayerController>();
                if (player != null) brain.AlertTo(player.transform.position);
            }
        }

        private int CountAlive()
        {
            alive.RemoveAll(e => e == null || !e.IsAlive || !e.gameObject.activeInHierarchy);
            return alive.Count;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
            if (spawnPoints.Length == 0) Gizmos.DrawWireCube(transform.position + Vector3.up, new Vector3(1f, 2f, 1f));
            foreach (Transform p in spawnPoints)
            {
                if (p != null) Gizmos.DrawWireCube(p.position + Vector3.up, new Vector3(1f, 2f, 1f));
            }
        }
    }
}
