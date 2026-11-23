using System.Collections.Generic;
using TheLostDungeon.Combat;
using TheLostDungeon.Core;
using TheLostDungeon.Events;
using TheLostDungeon.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TheLostDungeon.Abilities
{
    /// <summary>
    /// Two ability slots (Q / R, gamepad shoulder buttons, on-screen buttons on mobile).
    /// Holds abilities only through IAbility (Strategy pattern): swap Dash for another ability in
    /// the Inspector and nothing else changes.
    ///
    /// Locked abilities: an ability is usable if it's unlocked by default or its id is in the
    /// save's unlockedAbilities list (a level-completion reward). The HUD learns all of this
    /// through GameEvents.AbilityStatusChanged.
    /// </summary>
    public class PlayerAbilities : MonoBehaviour
    {
        [System.Serializable]
        private class Slot
        {
            [Tooltip("A component implementing IAbility (DashAbility, ShieldAbility...).")]
            public MonoBehaviour ability;
            public bool unlockedByDefault = true;
        }

        [SerializeField] private PlayerController controller;
        [SerializeField] private Transform view;
        [SerializeField] private Slot[] slots = new Slot[2];

        private static readonly string[] ActionNames = { "Ability1", "Ability2" };

        private readonly List<IAbility> abilities = new List<IAbility>();
        private readonly List<InputAction> actions = new List<InputAction>();
        private Health health;

        public IReadOnlyList<IAbility> Abilities => abilities;

        private void Awake()
        {
            if (controller == null) controller = GetComponent<PlayerController>();
            health = GetComponent<Health>();
            InputActionMap map = controller.Controls.FindActionMap("Player", true);

            for (int i = 0; i < slots.Length; i++)
            {
                IAbility ability = slots[i]?.ability as IAbility;
                abilities.Add(ability);
                actions.Add(i < ActionNames.Length ? map.FindAction(ActionNames[i], false) : null);
                if (ability is Ability concrete) concrete.StatusChanged += _ => Broadcast(ability);
            }
        }

        private void OnValidate()
        {
            if (slots == null) return;
            foreach (Slot s in slots)
            {
                if (s != null && s.ability != null && !(s.ability is IAbility)) s.ability = null;
            }
        }

        private void Start()
        {
            for (int i = 0; i < abilities.Count; i++) Broadcast(abilities[i]);   // initial HUD state
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            foreach (IAbility ability in abilities) ability?.Tick(dt);

            if (!CanUseAbilities()) return;

            var context = new AbilityContext(controller, health, view);
            for (int i = 0; i < abilities.Count; i++)
            {
                if (actions[i] != null && actions[i].WasPressedThisFrame()) TryUse(i, context);
            }
        }

        /// <summary>Also called by the mobile on-screen ability buttons.</summary>
        public bool TryUse(int slotIndex) =>
            CanUseAbilities() && TryUse(slotIndex, new AbilityContext(controller, health, view));

        public bool IsUnlocked(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Length || abilities[slotIndex] == null) return false;
            if (slots[slotIndex].unlockedByDefault) return true;
            return SaveManager.HasInstance && SaveManager.Instance.Data.unlockedAbilities.Contains(abilities[slotIndex].Id);
        }

        private bool TryUse(int slotIndex, in AbilityContext context)
        {
            if (!IsUnlocked(slotIndex)) return false;
            return abilities[slotIndex].TryActivate(context);
        }

        private bool CanUseAbilities()
        {
            if (controller != null && !controller.ControlEnabled) return false;
            if (health != null && !health.IsAlive) return false;
            return Time.timeScale > 0f;
        }

        private void Broadcast(IAbility ability)
        {
            int slot = abilities.IndexOf(ability);
            if (ability == null || slot < 0) return;

            GameEvents.RaiseAbilityStatusChanged(new AbilityStatus(
                slot, ability.Id, ability.DisplayName, ability.Icon, IsUnlocked(slot), ability.IsActive,
                ability.CooldownSeconds, ability.CooldownRemaining));
        }
    }
}
