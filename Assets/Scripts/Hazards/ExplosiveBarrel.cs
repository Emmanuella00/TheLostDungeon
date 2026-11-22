using System.Collections;
using System.Collections.Generic;
using TheLostDungeon.Algorithms;
using TheLostDungeon.Combat;
using TheLostDungeon.Pooling;
using UnityEngine;

namespace TheLostDungeon.Hazards
{
    /// <summary>
    /// A barrel that explodes when its Health runs out (shot, meleed, or caught in a blast).
    ///
    /// When the first barrel goes, the RECURSIVE ChainReaction algorithm works out every barrel that
    /// will be set off (neighbours of neighbours...), then they detonate one after another with a
    /// short delay for a ripple effect. Each blast damages every IDamageable in range (enemies,
    /// crates, the player!) with damage falling off toward the edge, and pushes physics props.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class ExplosiveBarrel : MonoBehaviour
    {
        private static readonly List<ExplosiveBarrel> activeBarrels = new List<ExplosiveBarrel>();

        [Header("Blast")]
        [SerializeField] private float blastRadius = 4f;
        [SerializeField] private float damage = 70f;
        [SerializeField, Range(0f, 1f)] private float edgeDamageFraction = 0.3f;
        [SerializeField] private float pushForce = 8f;
        [SerializeField] private LayerMask hitMask = ~0;

        [Header("Chain reaction")]
        [Tooltip("Other barrels this close are set off. Keep it at or below the blast radius.")]
        [SerializeField] private float chainRadius = 3.5f;
        [SerializeField, Min(0f)] private float delayBetweenBlasts = 0.15f;

        [Header("Effects")]
        [SerializeField] private GameObject explosionEffect;
        [SerializeField] private AudioClip explosionSound;

        private readonly Collider[] overlap = new Collider[32];
        private readonly HashSet<IDamageable> damagedThisBlast = new HashSet<IDamageable>();
        private Health health;
        private bool primed;   // part of a chain that is about to go off

        // FIX: Runner that never turns off, so chain coroutines never die
        // when a barrel is deactivated.
        private static MonoBehaviour _runner;
        private static MonoBehaviour Runner
        {
            get
            {
                if (_runner == null)
                {
                    var go = new GameObject("BarrelChainRunner");
                    DontDestroyOnLoad(go);
                    _runner = go.AddComponent<ChainRunner>();
                }
                return _runner;
            }
        }

        // Tiny helper that just lives forever so coroutines never die
        private class ChainRunner : MonoBehaviour { }

        private void Awake() => health = GetComponent<Health>();

        private void OnEnable()
        {
            activeBarrels.Add(this);
            health.Died += HandleDied;
        }

        private void OnDisable()
        {
            activeBarrels.Remove(this);
            health.Died -= HandleDied;
        }

        private void HandleDied()
        {
            if (!primed) StartChain(this);
        }

        private static void StartChain(ExplosiveBarrel first)
        {
            // Snapshot the barrels that can still explode, then let the recursive algorithm decide.
            var candidates = new List<ExplosiveBarrel>();
            var positions = new List<Vector3>();
            foreach (ExplosiveBarrel barrel in activeBarrels)
            {
                if (barrel == first || !barrel.primed)
                {
                    candidates.Add(barrel);
                    positions.Add(barrel.transform.position);
                }
            }

            List<int> order = ChainReaction.FindChain(candidates.IndexOf(first), positions, first.chainRadius);

            for (int i = 0; i < order.Count; i++)
            {
                ExplosiveBarrel barrel = candidates[order[i]];
                barrel.primed = true;    // so blast damage doesn't start a second chain from it
                // FIX: Start on Runner, NOT on the barrel itself.
                // If we start it on the barrel and the barrel gets
                // deactivated by an earlier blast, Unity throws:
                // "Coroutine couldn't be started because the game object is inactive!"
                Runner.StartCoroutine(barrel.DetonateAfter(i * first.delayBetweenBlasts));
            }
        }

        private IEnumerator DetonateAfter(float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            if (this == null) yield break;
            Detonate();
        }

        private void Detonate()
        {
            Vector3 centre = transform.position + Vector3.up * 0.5f;

            EffectPool.Spawn(explosionEffect, centre, Quaternion.identity);
            if (explosionSound != null) AudioSource.PlayClipAtPoint(explosionSound, centre);

            damagedThisBlast.Clear();
            int count = Physics.OverlapSphereNonAlloc(centre, blastRadius, overlap, hitMask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                Collider col = overlap[i];
                if (col.transform.IsChildOf(transform)) continue;

                // Barrels already in this chain will explode on their own; skip them.
                ExplosiveBarrel otherBarrel = col.GetComponentInParent<ExplosiveBarrel>();
                if (otherBarrel != null && otherBarrel.primed) continue;

                Vector3 point = col.ClosestPoint(centre);
                float distance = Vector3.Distance(centre, point);
                float falloff = Mathf.Lerp(1f, edgeDamageFraction, distance / blastRadius);

                IDamageable target = col.GetComponentInParent<IDamageable>();
                if (target != null && target.IsAlive && damagedThisBlast.Add(target))
                {
                    target.TakeDamage(new DamageInfo(damage * falloff, DamageType.Explosion, point, point - centre, pushForce, gameObject));
                }

                if (col.attachedRigidbody != null && !col.attachedRigidbody.isKinematic)
                {
                    col.attachedRigidbody.AddExplosionForce(pushForce * 50f, centre, blastRadius, 0.5f);
                }
            }

            gameObject.SetActive(false);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, blastRadius);
            Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, chainRadius);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => activeBarrels.Clear();
    }
}