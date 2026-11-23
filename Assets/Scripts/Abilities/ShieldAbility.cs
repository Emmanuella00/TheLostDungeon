using TheLostDungeon.Combat;
using UnityEngine;

namespace TheLostDungeon.Abilities
{
    /// <summary>
    /// An energy shield that absorbs most incoming damage for a few seconds. It works by
    /// temporarily raising the armour in the player's DefenceProfile, so the existing
    /// DamageCalculator does the maths: no special case anywhere in the damage system.
    /// </summary>
    public class ShieldAbility : Ability
    {
        [SerializeField, Min(0.5f)] private float duration = 4f;
        [SerializeField, Range(0f, 0.9f)] private float damageReduction = 0.75f;
        [Tooltip("Optional visual (e.g. a glowing bubble) shown while the shield is up.")]
        [SerializeField] private GameObject visual;

        private Health health;
        private DefenceProfile savedDefence;
        private float remaining;

        public override bool IsActive => remaining > 0f;

        private void Awake()
        {
            if (visual != null) visual.SetActive(false);
        }

        protected override bool CanActivate(in AbilityContext context) => context.Health != null && context.Health.IsAlive;

        protected override void OnActivate(in AbilityContext context)
        {
            health = context.Health;
            savedDefence = health.Defence;

            DefenceProfile boosted = savedDefence;
            boosted.armour = Mathf.Max(savedDefence.armour, damageReduction);
            health.Defence = boosted;

            remaining = duration;
            if (visual != null) visual.SetActive(true);
        }

        protected override void OnTick(float deltaTime)
        {
            if (remaining <= 0f) return;
            remaining -= deltaTime;
            if (remaining <= 0f) EndShield();
        }

        private void EndShield()
        {
            remaining = 0f;
            if (health != null) health.Defence = savedDefence;
            if (visual != null) visual.SetActive(false);
            NotifyStatusChanged();
        }

        private void OnDisable()
        {
            if (remaining > 0f) EndShield();
        }
    }
}
