using TheLostDungeon.Combat;
using TheLostDungeon.Events;
using TheLostDungeon.Pooling;
using UnityEngine;

namespace TheLostDungeon.Weapons
{
    /// <summary>
    /// A pooled energy bolt. Moves itself each frame with a sphere cast from its last position to
    /// its next one, so fast bolts can't pass through thin walls ("tunnelling") and no Rigidbody
    /// is needed. On impact it damages any IDamageable, spawns a pooled spark and returns to its pool.
    /// </summary>
    [RequireComponent(typeof(PooledObject))]
    public class Projectile : MonoBehaviour, IPoolable
    {
        [SerializeField, Min(0.01f)] private float radius = 0.06f;

        private readonly RaycastHit[] hits = new RaycastHit[8];

        private PooledObject pooled;
        private TrailRenderer trail;
        private GameObject owner;
        private PrefabPool impactPool;
        private Vector3 direction;
        private float speed;
        private float damage;
        private float knockback;
        private float distanceLeft;
        private LayerMask hitMask;
        private bool reportHits;
        private bool launched;

        private void Awake()
        {
            pooled = GetComponent<PooledObject>();
            trail = GetComponentInChildren<TrailRenderer>();
        }

        public void Launch(GameObject shooter, Vector3 dir, float boltSpeed, float boltDamage, float maxRange,
                           LayerMask mask, PrefabPool impacts, float boltKnockback = 0f, bool reportHitsToHud = false)
        {
            owner = shooter;
            direction = dir.normalized;
            speed = boltSpeed;
            damage = boltDamage;
            knockback = boltKnockback;
            distanceLeft = maxRange;
            hitMask = mask;
            impactPool = impacts;
            reportHits = reportHitsToHud;
            transform.rotation = Quaternion.LookRotation(direction);
            launched = true;
        }

        public void OnSpawn()
        {
            launched = false;
            if (trail != null) trail.Clear();
        }

        public void OnDespawn()
        {
            launched = false;
            owner = null;
        }

        private void Update()
        {
            if (!launched) return;

            float step = Mathf.Min(speed * Time.deltaTime, distanceLeft);
            Vector3 start = transform.position;

            if (TryFindHit(start, step, out RaycastHit hit))
            {
                transform.position = hit.point;
                Impact(hit);
                return;
            }

            transform.position = start + direction * step;
            distanceLeft -= step;
            if (distanceLeft <= 0f) pooled.ReturnToPool();
        }

        private bool TryFindHit(Vector3 start, float step, out RaycastHit best)
        {
            best = default;
            int count = Physics.SphereCastNonAlloc(start, radius, direction, hits, step, hitMask, QueryTriggerInteraction.Ignore);
            float bestDistance = float.PositiveInfinity;
            bool found = false;

            for (int i = 0; i < count; i++)
            {
                RaycastHit h = hits[i];
                if (owner != null && h.collider.transform.IsChildOf(owner.transform)) continue;   // never hit the shooter
                if (h.distance <= 0f && h.point == Vector3.zero) continue;                        // started inside a collider
                if (h.distance < bestDistance)
                {
                    bestDistance = h.distance;
                    best = h;
                    found = true;
                }
            }

            return found;
        }

        private void Impact(RaycastHit hit)
        {
            IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
            if (target != null && target.IsAlive)
            {
                var info = new DamageInfo(damage, DamageType.Ranged, hit.point, direction, knockback, owner);
                DamageResult result = target.TakeDamage(info);

                if (reportHits)
                {
                    GameEvents.RaiseDamageDealt(new DamageDealtInfo(result.FinalAmount, result.Blocked, !target.IsAlive, hit.point));
                }
            }

            if (impactPool != null)
            {
                Vector3 normal = hit.normal.sqrMagnitude > 0f ? hit.normal : -direction;
                impactPool.Spawn(hit.point + normal * 0.02f, Quaternion.LookRotation(normal));
            }

            pooled.ReturnToPool();
        }
    }
}
