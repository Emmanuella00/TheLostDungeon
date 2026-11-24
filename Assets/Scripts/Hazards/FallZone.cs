using System.Collections.Generic;
using TheLostDungeon.Combat;
using TheLostDungeon.Enemies;
using TheLostDungeon.Events;
using TheLostDungeon.Player;
using UnityEngine;

namespace TheLostDungeon.Hazards
{
    /// <summary>
    /// LOSE CONDITION for the rooftop: anything that drops below the kill height dies instantly.
    ///   - the player -> GameEvents.PlayerFell, then death -> Game Over ("You fell")
    ///   - an enemy   -> dies like any other kill, so it still counts toward "defeat all enemies"
    ///
    /// By default the kill height is set from where the player starts (start height minus
    /// Drop Allowed), so it works on any roof without measuring. Falling through a skylight into
    /// the building counts as falling too. The red plane in the Scene view shows the height.
    /// Put ONE FallZone anywhere in the level.
    /// </summary>
    public class FallZone : MonoBehaviour
    {
        [Tooltip("Work out the kill height from the player's starting height.")]
        [SerializeField] private bool relativeToPlayerStart = true;

        [Tooltip("How far below the start height the player may drop before dying (steps, small ledges).")]
        [SerializeField, Min(0.5f)] private float dropAllowed = 4f;

        [Tooltip("Used when 'Relative To Player Start' is off: the world height (Y) of death.")]
        [SerializeField] private float killHeight = -10f;

        [SerializeField] private float enemyCheckInterval = 0.5f;

        private PlayerController player;
        private Health playerHealth;
        private float nextEnemyCheck;
        private readonly List<EnemyBrain> enemies = new List<EnemyBrain>();

        public float KillHeight => killHeight;

        private void Start()
        {
            player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                playerHealth = player.GetComponent<Health>();
                if (relativeToPlayerStart) killHeight = player.transform.position.y - dropAllowed;
            }
        }

        private void Update()
        {
            if (player != null && playerHealth != null && playerHealth.IsAlive && player.transform.position.y < killHeight)
            {
                GameEvents.RaisePlayerFell();   // so the Game Over screen can say why
                playerHealth.Kill();
            }

            if (Time.time >= nextEnemyCheck)
            {
                nextEnemyCheck = Time.time + enemyCheckInterval;
                KillFallenEnemies();
            }
        }

        private void KillFallenEnemies()
        {
            enemies.Clear();
            enemies.AddRange(FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None));
            foreach (EnemyBrain enemy in enemies)
            {
                if (!enemy.IsAlive || enemy.transform.position.y >= killHeight) continue;
                Health health = enemy.GetComponent<Health>();
                if (health != null) health.Kill();
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            float height = killHeight;
            if (relativeToPlayerStart && !Application.isPlaying)
            {
                PlayerController scenePlayer = FindAnyObjectByType<PlayerController>();
                if (scenePlayer == null) return;
                height = scenePlayer.transform.position.y - dropAllowed;
            }

            Vector3 centre = new Vector3(transform.position.x, height, transform.position.z);
            Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.25f);
            Gizmos.DrawCube(centre, new Vector3(200f, 0.05f, 200f));
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(centre, new Vector3(200f, 0.05f, 200f));
        }
#endif
    }
}
