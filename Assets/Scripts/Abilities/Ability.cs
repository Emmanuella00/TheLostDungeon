using System;
using TheLostDungeon.Audio;
using TheLostDungeon.Events;
using UnityEngine;

namespace TheLostDungeon.Abilities
{
    /// <summary>
    /// Shared base for abilities (TEMPLATE METHOD): handles the cooldown, sound and events, so
    /// each concrete ability only writes what makes it unique in OnActivate / OnTick.
    /// </summary>
    public abstract class Ability : MonoBehaviour, IAbility
    {
        [SerializeField] private string abilityId = "ability";
        [SerializeField] private string displayName = "Ability";
        [SerializeField] private Sprite icon;
        [SerializeField, Min(0.1f)] private float cooldownSeconds = 5f;
        [SerializeField] private AudioClip activateSound;

        private Cooldown cooldown;

        /// <summary>Raised when the ability is used, finishes its effect, or comes off cooldown.</summary>
        public event Action<IAbility> StatusChanged;

        public string Id => abilityId;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public float CooldownSeconds => cooldownSeconds;
        public float CooldownRemaining => Timer.Remaining;
        public bool IsReady => Timer.IsReady && !IsActive;
        public abstract bool IsActive { get; }

        private Cooldown Timer
        {
            get
            {
                if (cooldown == null) cooldown = new Cooldown(cooldownSeconds);
                return cooldown;
            }
        }

        public bool TryActivate(in AbilityContext context)
        {
            if (!IsReady || !CanActivate(context)) return false;

            OnActivate(context);
            Timer.Start();

            if (activateSound != null && AudioManager.HasInstance) AudioManager.Instance.PlaySfx(activateSound);
            GameEvents.RaiseAbilityActivated(abilityId, cooldownSeconds);
            NotifyStatusChanged();
            return true;
        }

        public void Tick(float deltaTime)
        {
            OnTick(deltaTime);
            if (Timer.Tick(deltaTime)) NotifyStatusChanged();   // ready again
        }

        protected virtual bool CanActivate(in AbilityContext context) => true;
        protected abstract void OnActivate(in AbilityContext context);
        protected virtual void OnTick(float deltaTime) { }

        protected void NotifyStatusChanged() => StatusChanged?.Invoke(this);

#if UNITY_EDITOR
        public void EditorConfigure(string id, string name, float cooldown)
        {
            abilityId = id;
            displayName = name;
            cooldownSeconds = cooldown;
        }
#endif
    }
}
