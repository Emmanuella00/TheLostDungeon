using TheLostDungeon.Combat;
using TheLostDungeon.Player;
using UnityEngine;

namespace TheLostDungeon.Abilities
{
    /// <summary>
    /// STRATEGY PATTERN for player abilities. PlayerAbilities holds abilities through this
    /// interface and never knows what they do: Dash and Shield are interchangeable strategies,
    /// and a third ability (grenade, slow-time) is a new class with no changes elsewhere.
    /// </summary>
    public interface IAbility
    {
        string Id { get; }
        string DisplayName { get; }
        Sprite Icon { get; }
        float CooldownSeconds { get; }
        float CooldownRemaining { get; }
        bool IsReady { get; }
        bool IsActive { get; }

        bool TryActivate(in AbilityContext context);
        void Tick(float deltaTime);
    }

    /// <summary>What an ability may use: the player's body, health and view.</summary>
    public readonly struct AbilityContext
    {
        public readonly PlayerController Player;
        public readonly Health Health;
        public readonly Transform View;

        public AbilityContext(PlayerController player, Health health, Transform view)
        {
            Player = player;
            Health = health;
            View = view;
        }
    }
}
