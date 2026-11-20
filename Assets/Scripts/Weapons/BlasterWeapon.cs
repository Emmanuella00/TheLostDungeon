using System;
using TheLostDungeon.Algorithms;
using TheLostDungeon.Events;
using TheLostDungeon.Pooling;
using UnityEngine;

namespace TheLostDungeon.Weapons
{
    /// <summary>
    /// The player's main weapon: fires pooled energy bolts and overheats instead of using ammo.
    ///
    /// Aiming: bolts leave the muzzle but fly toward whatever is under the crosshair (a ray from
    /// the camera), so shots land where the player is looking.
    /// Aim assist (mobile by default): if an enemy is inside a small cone around the crosshair,
    /// the bolt is aimed at the nearest one (TargetFinder algorithm).
    /// </summary>
    public class BlasterWeapon : MonoBehaviour, IWeapon
    {
        [Header("References")]
        [SerializeField] private Transform muzzle;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private GameObject impactEffectPrefab;
        [SerializeField] private GameObject muzzleFlashPrefab;

        [Header("Shooting")]
        [SerializeField] private float damage = 20f;
        [SerializeField] private float projectileSpeed = 45f;
        [SerializeField] private float range = 80f;
        [SerializeField, Min(0.02f)] private float secondsBetweenShots = 0.15f;
        [SerializeField] private LayerMask hitMask = ~0;

        [Header("Heat")]
        [SerializeField, Range(0.01f, 1f)] private float heatPerShot = 0.08f;
        [SerializeField, Min(0f)] private float coolPerSecond = 0.35f;
        [SerializeField, Min(0.1f)] private float overheatLockSeconds = 1.5f;

        [Header("Aim assist")]
        [SerializeField] private bool aimAssistOnDesktop = false;
        [SerializeField, Range(1f, 30f)] private float aimAssistConeDegrees = 8f;
        [SerializeField] private float aimAssistRange = 40f;

        [Header("Pooling")]
        [SerializeField, Min(0)] private int prewarmBolts = 20;

        private HeatGauge heat;
        private float nextShotTime;
        private float lastReportedHeat = -1f;
        private bool lastReportedOverheat;
        private Transform poolContainer;
        private PrefabPool boltPool;
        private PrefabPool impactPool;
        private PrefabPool flashPool;

        /// <summary>Local event for the weapon view model (recoil kick).</summary>
        public event Action Fired;

        public string Id => "blaster";
        public bool IsReady => heat.CanFire && Time.time >= nextShotTime;
        public float Heat => heat.Heat;
        public bool IsOverheated => heat.IsOverheated;

        private void Awake()
        {
            heat = new HeatGauge(coolPerSecond, overheatLockSeconds);

            // Pooled bolts live at the scene root so they don't move with the player.
            poolContainer = new GameObject($"[Pool] {name}").transform;
            if (projectilePrefab != null) boltPool = new PrefabPool(projectilePrefab, prewarmBolts, poolContainer);
            if (impactEffectPrefab != null) impactPool = new PrefabPool(impactEffectPrefab, prewarmBolts / 2, poolContainer);
            if (muzzleFlashPrefab != null) flashPool = new PrefabPool(muzzleFlashPrefab, 4, poolContainer);
        }

        private void OnDestroy()
        {
            if (poolContainer != null) Destroy(poolContainer.gameObject);
        }

        public void Tick(float deltaTime)
        {
            heat.Cool(deltaTime);
            ReportHeatIfChanged();
        }

        public bool TryUse(in WeaponContext context)
        {
            if (!IsReady || boltPool == null || context.AimOrigin == null) return false;

            nextShotTime = Time.time + secondsBetweenShots;
            Vector3 origin = muzzle != null ? muzzle.position : context.AimOrigin.position;
            Vector3 direction = (GetAimPoint(context) - origin).normalized;

            GameObject bolt = boltPool.Spawn(origin, Quaternion.LookRotation(direction));
            if (bolt != null && bolt.TryGetComponent(out Projectile projectile))
            {
                projectile.Launch(context.Owner, direction, projectileSpeed, damage, range, hitMask, impactPool, 0f, true);
            }

            flashPool?.Spawn(origin, Quaternion.LookRotation(direction));

            heat.AddHeat(heatPerShot);
            ReportHeatIfChanged();
            Fired?.Invoke();
            GameEvents.RaiseWeaponFired(Id);
            return true;
        }

        private Vector3 GetAimPoint(in WeaponContext context)
        {
            Transform aim = context.AimOrigin;

            if (UseAimAssist())
            {
                ITargetable target = TargetRegistry.FindNearest(aim.position, aim.forward, aimAssistRange, aimAssistConeDegrees);
                if (target != null) return target.AimPoint;
            }

            // Whatever is under the crosshair, ignoring the player's own colliders.
            var ray = new Ray(aim.position, aim.forward);
            RaycastHit[] results = Physics.RaycastAll(ray, range, hitMask, QueryTriggerInteraction.Ignore);
            float nearest = range;
            Vector3 point = ray.GetPoint(range);
            foreach (RaycastHit hit in results)
            {
                if (context.Owner != null && hit.collider.transform.IsChildOf(context.Owner.transform)) continue;
                if (hit.distance < nearest)
                {
                    nearest = hit.distance;
                    point = hit.point;
                }
            }
            return point;
        }

        /// <summary>
        /// CONDITIONAL COMPILATION: aim assist is always on for touch screens, where precise aiming
        /// is hard, and optional on PC/WebGL (off by default for mouse players).
        /// </summary>
        private bool UseAimAssist()
        {
#if UNITY_ANDROID || UNITY_IOS
            return true;
#else
            return aimAssistOnDesktop;
#endif
        }

        private void ReportHeatIfChanged()
        {
            if (Mathf.Abs(heat.Heat - lastReportedHeat) < 0.01f && heat.IsOverheated == lastReportedOverheat) return;
            lastReportedHeat = heat.Heat;
            lastReportedOverheat = heat.IsOverheated;
            GameEvents.RaiseWeaponHeatChanged(heat.Heat, heat.IsOverheated);
        }
    }
}
