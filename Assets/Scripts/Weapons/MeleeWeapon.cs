using System;
using System.Collections.Generic;
using TheLostDungeon.Combat;
using TheLostDungeon.Events;
using UnityEngine;

namespace TheLostDungeon.Weapons
{
    /// <summary>
    /// Close-range gun bash. Heavy damage, knockback, and it breaks Guardian shields (melee hits
    /// are never blocked by front shields; see DamageCalculator).
    ///
    /// Timing: the hit lands partway through the swing. If the view model has a melee animation
    /// with an Animation Event calling AnimationEventHit(), set useAnimationEvent so damage lines
    /// up exactly with the animation; otherwise a simple delay is used.
    /// </summary>
    public class MeleeWeapon : MonoBehaviour, IWeapon
    {
        [SerializeField] private float damage = 45f;
        [SerializeField] private float knockback = 6f;
        [SerializeField] private float reach = 1.8f;
        [SerializeField] private float hitRadius = 0.9f;
        [SerializeField, Range(10f, 180f)] private float hitArcDegrees = 100f;
        [SerializeField, Min(0.1f)] private float cooldownSeconds = 0.6f;
        [SerializeField, Min(0f)] private float hitDelaySeconds = 0.12f;
        [SerializeField] private bool useAnimationEvent = false;
        [SerializeField] private LayerMask hitMask = ~0;

        private readonly Collider[] overlapBuffer = new Collider[16];
        private readonly HashSet<IDamageable> alreadyHit = new HashSet<IDamageable>();

        private float readyTime;
        private float pendingHitTime = -1f;
        private WeaponContext pendingContext;

        /// <summary>Local event for the view model (plays the swing).</summary>
        public event Action Swung;

        public string Id => "melee";
        public bool IsReady => Time.time >= readyTime;
        public float CooldownNormalized => Mathf.Clamp01((readyTime - Time.time) / cooldownSeconds);

        public void Tick(float deltaTime)
        {
            if (pendingHitTime >= 0f && !useAnimationEvent && Time.time >= pendingHitTime)
            {
                pendingHitTime = -1f;
                PerformHit(pendingContext);
            }
        }

        public bool TryUse(in WeaponContext context)
        {
            if (!IsReady || context.AimOrigin == null) return false;

            readyTime = Time.time + cooldownSeconds;
            pendingContext = context;
            pendingHitTime = Time.time + hitDelaySeconds;
            Swung?.Invoke();
            GameEvents.RaiseWeaponFired(Id);
            return true;
        }

        /// <summary>Call this from an Animation Event on the melee clip's impact frame.</summary>
        public void AnimationEventHit()
        {
            if (!useAnimationEvent || pendingHitTime < 0f) return;
            pendingHitTime = -1f;
            PerformHit(pendingContext);
        }

        private void PerformHit(in WeaponContext context)
        {
            if (context.AimOrigin == null) return;

            Transform aim = context.AimOrigin;
            Vector3 centre = aim.position + aim.forward * (reach * 0.5f);
            int count = Physics.OverlapSphereNonAlloc(centre, hitRadius + reach * 0.5f, overlapBuffer, hitMask, QueryTriggerInteraction.Ignore);
            float minDot = Mathf.Cos(hitArcDegrees * 0.5f * Mathf.Deg2Rad);
            alreadyHit.Clear();

            for (int i = 0; i < count; i++)
            {
                Collider col = overlapBuffer[i];
                if (context.Owner != null && col.transform.IsChildOf(context.Owner.transform)) continue;

                IDamageable target = col.GetComponentInParent<IDamageable>();
                if (target == null || !target.IsAlive || alreadyHit.Contains(target)) continue;

                Vector3 point = col.ClosestPoint(aim.position);
                Vector3 toTarget = point - aim.position;
                if (toTarget.sqrMagnitude > reach * reach * 1.5f) continue;
                if (toTarget.sqrMagnitude > 0.0001f && Vector3.Dot(aim.forward, toTarget.normalized) < minDot) continue;

                alreadyHit.Add(target);
                var info = new DamageInfo(damage, DamageType.Melee, point, aim.forward, knockback, context.Owner);
                DamageResult result = target.TakeDamage(info);

                // Physics props (crates, barrels) get pushed. Enemies will read Knockback themselves.
                if (col.attachedRigidbody != null && !col.attachedRigidbody.isKinematic)
                {
                    col.attachedRigidbody.AddForceAtPosition(aim.forward * knockback, point, ForceMode.Impulse);
                }

                GameEvents.RaiseDamageDealt(new DamageDealtInfo(result.FinalAmount, result.Blocked, !target.IsAlive, point));
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.35f);
            Transform t = transform;
            Gizmos.DrawWireSphere(t.position + t.forward * (reach * 0.5f), hitRadius + reach * 0.5f);
        }
    }
}
