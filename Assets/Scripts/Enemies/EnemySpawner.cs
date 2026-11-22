using System.Collections;
using System.Collections.Generic;
using TheLostDungeon.Interaction;
using TheLostDungeon.Player;
using TheLostDungeon.Pooling;
using UnityEngine;
using UnityEngine.AI;

namespace TheLostDungeon.Enemies
{
    /// <summary>
    /// Spawns waves of enemies from OBJECT POOLS: dead enemies go back to the pool and are reused
    /// for the next spawn instead of being destroyed and re-created.
    ///
    /// It implements IActivatable, so the EXISTING WallSwitch (or an ActivationZone trigger) can
    /// start a wave with no new code: "pull the lockdown lever, enemies pour in".
    /// </summary>
    public class EnemySpawner : MonoBehaviour, IActivatable
    {
        [Tooltip("Enemy prefabs to spawn, cycled in order (e.g. Warrior, Warrior, Archer).")]
        [SerializeField] private GameObject[] enemyPrefabs = new GameObject[0];
        [Tooltip("Where they appear, cycled in order. Empty = this object's position.")]
        [SerializeField] private Transform[] spawnPoints = new Transform[0];

        [SerializeField, Min(1)] private int totalToSpawn = 6;
        [SerializeField, Min(1)] private int maxAliveAtOnce = 3;
        [SerializeField, Min(0.1f)] private float secondsBetweenSpawns = 2f;
        [SerializeField] private bool startOnPlay;
        [Tooltip("New enemies head straight for the player instead of patrolling.")]
        [SerializeField] private bool alertOnSpawn = true;
        [SerializeField, Min(0)] private int prewarmPerPrefab = 2;

        private readonly Dictionary<GameObject, PrefabPool> pools = new Dictionary<GameObject, PrefabPool>();
        private readonly List<EnemyBrain> alive = new List<EnemyBrain>();
        private Transform container;
        private Coroutine routine;
        private int spawned;

        public int Spawned => spawned;
        public bool IsRunning => routine != null;
        public bool Finished => spawned >= totalToSpawn && CountAlive() == 0;

        private void Awake()
        {
            container = new GameObject($"[Pool] {name}").transform;
            foreach (GameObject prefab in enemyPrefabs)
            {
                if (prefab != null && !pools.ContainsKey(prefab)) pools[prefab] = new PrefabPool(prefab, prewarmPerPrefab, container);
            }
        }

        private void Start()
        {
            if (startOnPlay) SetActivated(true);
        }

        private void OnDestroy()
        {
            if (container != null) Destroy(container.gameObject);
        }

        public void SetActivated(bool activated)
        {
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
            if (enemyPrefabs.Length == 0) return;

            GameObject prefab = enemyPrefabs[spawned % enemyPrefabs.Length];
            Transform point = spawnPoints.Length > 0 ? spawnPoints[spawned % spawnPoints.Length] : transform;
            spawned++;
            if (prefab == null || point == null) return;

            Vector3 position = point.position;
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, 3f, NavMesh.AllAreas)) position = hit.position;

            GameObject enemy = pools[prefab].Spawn(position, point.rotation);
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
