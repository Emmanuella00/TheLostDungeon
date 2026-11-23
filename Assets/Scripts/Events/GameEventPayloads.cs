using UnityEngine;

namespace TheLostDungeon.Events
{
    // Small immutable structs carried by events. Using structs (not classes) means raising
    // an event allocates no garbage, which matters for WebGL and mobile.

    public readonly struct EnemyDefeatedInfo
    {
        public readonly string EnemyId;
        public readonly Vector3 Position;
        public readonly int ScoreValue;

        public EnemyDefeatedInfo(string enemyId, Vector3 position, int scoreValue)
        {
            EnemyId = enemyId;
            Position = position;
            ScoreValue = scoreValue;
        }
    }

    public readonly struct ItemCollectedInfo
    {
        public readonly string ItemId;
        public readonly int Amount;

        public ItemCollectedInfo(string itemId, int amount)
        {
            ItemId = itemId;
            Amount = amount;
        }
    }

    /// <summary>Sent when the player's attack lands, used for crosshair hit markers.</summary>
    public readonly struct DamageDealtInfo
    {
        public readonly float Amount;
        public readonly bool Blocked;
        public readonly bool Killed;
        public readonly Vector3 Point;

        public DamageDealtInfo(float amount, bool blocked, bool killed, Vector3 point)
        {
            Amount = amount;
            Blocked = blocked;
            Killed = killed;
            Point = point;
        }
    }

    /// <summary>Everything the HUD needs to draw one ability icon.</summary>
    public readonly struct AbilityStatus
    {
        public readonly int Slot;
        public readonly string AbilityId;
        public readonly string DisplayName;
        public readonly Sprite Icon;
        public readonly bool Unlocked;
        public readonly bool Active;               // effect currently running (e.g. shield up)
        public readonly float CooldownSeconds;
        public readonly float CooldownRemaining;

        public AbilityStatus(int slot, string abilityId, string displayName, Sprite icon, bool unlocked, bool active,
                             float cooldownSeconds, float cooldownRemaining)
        {
            Slot = slot;
            AbilityId = abilityId;
            DisplayName = displayName;
            Icon = icon;
            Unlocked = unlocked;
            Active = active;
            CooldownSeconds = cooldownSeconds;
            CooldownRemaining = cooldownRemaining;
        }
    }

    public readonly struct LevelResult
    {
        public readonly int LevelIndex;
        public readonly float TimeSeconds;
        public readonly int Score;
        public readonly int Stars;
        public readonly bool UnlockedNewLevel;

        public LevelResult(int levelIndex, float timeSeconds, int score, int stars, bool unlockedNewLevel)
        {
            LevelIndex = levelIndex;
            TimeSeconds = timeSeconds;
            Score = score;
            Stars = stars;
            UnlockedNewLevel = unlockedNewLevel;
        }
    }
}
