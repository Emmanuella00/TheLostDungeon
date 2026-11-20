using TheLostDungeon.Algorithms;
using TheLostDungeon.Events;
using UnityEngine;

namespace TheLostDungeon.Combat
{
    /// <summary>
    /// A practice dummy for testing combat before real enemies exist (step 4).
    /// Counts as an enemy: it can be aim-assisted, raises EnemyDefeated when destroyed (so the
    /// stats counter and sounds react), then respawns after a few seconds.
    /// Give it a front shield in its Health "Defence" settings to test Guardian blocking.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class TrainingTarget : MonoBehaviour, ITargetable
    {
        [SerializeField] private string enemyId = "training_dummy";
        [SerializeField] private int scoreValue = 10;
        [SerializeField, Min(0.5f)] private float respawnSeconds = 3f;
        [SerializeField] private Transform aimPoint;
        [SerializeField] private GameObject visualRoot;

        private Health health;
        private Collider[] colliders;
        private float respawnAt = -1f;

        public Vector3 AimPoint => aimPoint != null ? aimPoint.position : transform.position + Vector3.up;
        public bool IsTargetable => health != null && health.IsAlive;

        private void Awake()
        {
            health = GetComponent<Health>();
            colliders = GetComponentsInChildren<Collider>();
            if (visualRoot == null) visualRoot = gameObject;
        }

        private void OnEnable()
        {
            health.Died += HandleDied;
            TargetRegistry.Register(this);
        }

        private void OnDisable()
        {
            health.Died -= HandleDied;
            TargetRegistry.Unregister(this);
        }

        private void HandleDied()
        {
            GameEvents.RaiseEnemyDefeated(new EnemyDefeatedInfo(enemyId, transform.position, scoreValue));
            SetVisible(false);
            respawnAt = Time.time + respawnSeconds;
        }

        private void Update()
        {
            if (respawnAt > 0f && Time.time >= respawnAt)
            {
                respawnAt = -1f;
                health.Revive();
                SetVisible(true);
            }
        }

        private void SetVisible(bool visible)
        {
            foreach (Renderer r in visualRoot.GetComponentsInChildren<Renderer>()) r.enabled = visible;
            foreach (Collider c in colliders) c.enabled = visible;
        }
    }
}
