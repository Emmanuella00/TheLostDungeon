using TheLostDungeon.Combat;
using TheLostDungeon.Events;
using UnityEngine;

namespace TheLostDungeon.Player
{
    /// <summary>
    /// Connects the player's local Health component to the global GameEvents, so the HUD,
    /// GameManager (game over), audio and stats can react to the player's health without
    /// referencing the player object at all.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class PlayerHealthBridge : MonoBehaviour
    {
        private Health health;
        private PlayerController controller;

        private void Awake()
        {
            health = GetComponent<Health>();
            controller = GetComponent<PlayerController>();
        }

        private void OnEnable()
        {
            health.Changed += HandleChanged;
            health.Died += HandleDied;
            health.Damaged += HandleDamaged;
        }

        private void OnDisable()
        {
            health.Changed -= HandleChanged;
            health.Died -= HandleDied;
            health.Damaged -= HandleDamaged;
        }

        /// <summary>Hits with knockback (shield bash, explosions) push the player.</summary>
        private void HandleDamaged(DamageInfo info, DamageResult result)
        {
            if (controller != null && info.Knockback > 0f) controller.AddKnockback(info.Direction * info.Knockback);
        }

        private void Start() => HandleChanged(health.Current, health.Max);   // initial HUD value

        private static void HandleChanged(float current, float max) =>
            GameEvents.RaisePlayerHealthChanged(Mathf.CeilToInt(current), Mathf.CeilToInt(max));

        private static void HandleDied() => GameEvents.RaisePlayerDied();
    }
}
