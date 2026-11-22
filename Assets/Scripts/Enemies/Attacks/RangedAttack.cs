using System.Collections;
using TheLostDungeon.Pooling;
using TheLostDungeon.Weapons;
using UnityEngine;

namespace TheLostDungeon.Enemies
{
    /// <summary>
    /// Archer attack: charges up briefly (matches the drone's Charging animation), then fires a
    /// POOLED bolt at the player with a little inaccuracy. Reuses the player's Projectile class:
    /// the same bolt code hits the player, enemies or explosive barrels.
    /// </summary>
    public class RangedAttack : EnemyAttack
    {
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform muzzle;
        [SerializeField] private float damage = 10f;
        [SerializeField] private float projectileSpeed = 22f;
        [SerializeField] private float range = 40f;
        [SerializeField, Range(0f, 10f)] private float spreadDegrees = 2.5f;
        [SerializeField, Min(0f)] private float chargeSeconds = 0.4f;
        [SerializeField] private LayerMask hitMask = ~0;
        [SerializeField] private AudioClip fireSound;

        private static Transform sharedContainer;
        private PrefabPool pool;

        private void Awake()
        {
            if (projectilePrefab == null) return;
            if (sharedContainer == null) sharedContainer = new GameObject("[Pool] Enemy Bolts").transform;
            pool = new PrefabPool(projectilePrefab, 6, sharedContainer);
        }

        public override void Execute(EnemyBrain attacker, Transform target, Vector3 targetAimPoint)
        {
            if (pool != null) StartCoroutine(ChargeAndFire(attacker, targetAimPoint));
        }

        private IEnumerator ChargeAndFire(EnemyBrain attacker, Vector3 aimPoint)
        {
            if (chargeSeconds > 0f) yield return new WaitForSeconds(chargeSeconds);
            if (!attacker.IsAlive) yield break;

            // Re-aim at the player's CURRENT position so moving targets are harder to hit only by spread.
            Vector3 currentAim = attacker.TargetAimPoint ?? aimPoint;
            Vector3 origin = muzzle != null ? muzzle.position : attacker.EyePosition;
            Vector3 direction = (currentAim - origin).normalized;
            direction = Quaternion.Euler(Random.Range(-spreadDegrees, spreadDegrees), Random.Range(-spreadDegrees, spreadDegrees), 0f) * direction;

            GameObject bolt = pool.Spawn(origin, Quaternion.LookRotation(direction));
            if (bolt != null && bolt.TryGetComponent(out Projectile projectile))
            {
                projectile.Launch(attacker.gameObject, direction, projectileSpeed, damage, range, hitMask, null);
            }

            if (fireSound != null) attacker.PlaySound(fireSound);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => sharedContainer = null;
    }
}
