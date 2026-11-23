using System;
using UnityEngine;

namespace TheLostDungeon.Combat
{
    /// <summary>
    /// Reusable health component for the player, enemies and props. Implements IDamageable.
    /// Defences (armour, shields) are set per object in the Inspector.
    ///
    /// Other components react through its C# events (Damaged, Died, Changed) instead of this
    /// class knowing about them. E.g. PlayerHealthBridge forwards the player's events to
    /// GameEvents, and DamageFlash flashes the model red.
    /// </summary>
    public class Health : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField] private DefenceProfile defence;

        [Tooltip("Seconds of invulnerability after being hit (stops one explosion hitting many times).")]
        [SerializeField, Min(0f)] private float invulnerabilitySeconds = 0f;

        private HealthModel model;
        private float invulnerableUntil;

        public event Action<DamageInfo, DamageResult> Damaged;
        public event Action<float, float> Changed;      // (current, max)
        public event Action Died;

        public bool IsAlive => Model.IsAlive;
        public float Current => Model.Current;
        public float Max => Model.Max;
        public float Normalized => Model.Normalized;
        public DefenceProfile Defence { get => defence; set => defence = value; }

        private HealthModel Model
        {
            get
            {
                if (model == null) CreateModel();
                return model;
            }
        }

        private void Awake()
        {
            if (model == null) CreateModel();
        }

        private void CreateModel()
        {
            model = new HealthModel(maxHealth);
            model.Changed += (current, max) => Changed?.Invoke(current, max);
            model.Died += () => Died?.Invoke();
        }

        public DamageResult TakeDamage(in DamageInfo info)
        {
            if (!IsAlive || Time.time < invulnerableUntil) return new DamageResult(0f, false, false);

            DamageResult result = DamageCalculator.Calculate(info, defence, transform.forward);
            if (result.FinalAmount > 0f && invulnerabilitySeconds > 0f) invulnerableUntil = Time.time + invulnerabilitySeconds;

            Damaged?.Invoke(info, result);
            Model.Damage(result.FinalAmount);   // raises Changed, and Died if it reached zero
            return result;
        }

        /// <summary>Ignore all damage for a while (e.g. during a dash).</summary>
        public void SetInvulnerable(float seconds) => invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + seconds);

        public float Heal(float amount) => Model.Heal(amount);
        public void Revive() => Model.Revive();
        public void SetMaxHealth(float newMax, bool fillToMax) => Model.SetMax(newMax, fillToMax);
    }
}
