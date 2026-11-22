using System.Collections;
using TheLostDungeon.Combat;
using UnityEngine;

namespace TheLostDungeon.Enemies
{
    /// <summary>
    /// Close-range strike (Warrior). The hit lands after a short wind-up that matches the Attack
    /// animation, and only if the player is still in reach and in front: you can dodge by backing off.
    /// </summary>
    public class MeleeAttack : EnemyAttack
    {
        [SerializeField] protected float damage = 15f;
        [SerializeField] protected float reach = 2.2f;
        [SerializeField, Range(30f, 180f)] protected float hitArcDegrees = 90f;
        [SerializeField, Min(0f)] protected float windUpSeconds = 0.35f;
        [SerializeField] protected float knockback = 3f;
        [SerializeField] protected AudioClip attackSound;

        public override void Execute(EnemyBrain attacker, Transform target, Vector3 targetAimPoint)
        {
            if (attackSound != null) attacker.PlaySound(attackSound);
            StartCoroutine(StrikeAfterWindUp(attacker, target));
        }

        protected virtual IEnumerator StrikeAfterWindUp(EnemyBrain attacker, Transform target)
        {
            if (windUpSeconds > 0f) yield return new WaitForSeconds(windUpSeconds);
            if (!attacker.IsAlive || target == null) yield break;

            Vector3 origin = attacker.transform.position;
            Vector3 toTarget = target.position - origin;
            toTarget.y = 0f;

            bool inReach = toTarget.sqrMagnitude <= reach * reach;
            bool inFront = EnemyMath.IsInViewCone(origin, attacker.transform.forward, target.position, reach, hitArcDegrees);
            if (!inReach || !inFront) yield break;   // dodged

            IDamageable victim = target.GetComponentInParent<IDamageable>();
            victim?.TakeDamage(new DamageInfo(damage, DamageType.Melee, target.position + Vector3.up, toTarget, knockback, attacker.gameObject));
        }
    }
}
