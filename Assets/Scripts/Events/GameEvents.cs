using System;
using TheLostDungeon.Core;
using UnityEngine;

namespace TheLostDungeon.Events
{
    /// <summary>
    /// OBSERVER PATTERN — the game's central event hub (delegates + C# events).
    ///
    /// Publishers call a Raise method and don't know who is listening. Subscribers (UI, audio,
    /// save system, objective tracker, ...) subscribe in OnEnable and unsubscribe in OnDisable.
    ///
    /// Example: an enemy dies and calls RaiseEnemyDefeated(...). The HUD, the AudioManager and
    /// the PlayerStatsTracker all react, yet the enemy script references none of them. Any
    /// listener can be removed or replaced without touching the enemy code.
    ///
    /// Rule of thumb: ALWAYS unsubscribe in OnDisable, otherwise a destroyed object will still
    /// be called after a scene change (MissingReferenceException).
    /// </summary>
    public static class GameEvents
    {
        // ----- Game flow -----
        public static event Action<GameState, GameState> GameStateChanged;   // (previous, current)
        public static event Action<float> LevelLoadProgress;                // 0..1
        public static event Action<string> LevelLoadFailed;                 // reason
        public static event Action<int> LevelStarted;                       // level index
        public static event Action<LevelResult> LevelCompleted;

        // ----- Player -----
        public static event Action<int, int> PlayerHealthChanged;           // (current, max)
        public static event Action PlayerDied;
        public static event Action PlayerFell;                              // fell off the level (rooftop)
        public static event Action<string, float> AbilityActivated;         // (abilityId, cooldown)
        public static event Action<AbilityStatus> AbilityStatusChanged;     // unlocked / used / ready again
        public static event Action<string> WeaponFired;                     // weaponId
        public static event Action<float, bool> WeaponHeatChanged;          // (heat 0..1, overheated)
        public static event Action<DamageDealtInfo> DamageDealt;            // player's hit landed

        // ----- World -----
        public static event Action<EnemyDefeatedInfo> EnemyDefeated;
        public static event Action<ItemCollectedInfo> ItemCollected;
        public static event Action<string, int, int> ObjectiveUpdated;      // (description, current, target)
        public static event Action<string> InteractionPromptChanged;        // prompt text, or null to hide
        public static event Action InventoryChanged;
        public static event Action<string, bool> SwitchActivated;           // (switchId, isOn)
        public static event Action<Vector3, float> NoiseMade;               // (position, radius) gunshots, explosions

        // ----- Systems -----
        public static event Action ProgressSaved;
        public static event Action SettingsChanged;

        public static void RaiseGameStateChanged(GameState previous, GameState current) => GameStateChanged?.Invoke(previous, current);
        public static void RaiseLevelLoadProgress(float progress) => LevelLoadProgress?.Invoke(progress);
        public static void RaiseLevelLoadFailed(string reason) => LevelLoadFailed?.Invoke(reason);
        public static void RaiseLevelStarted(int levelIndex) => LevelStarted?.Invoke(levelIndex);
        public static void RaiseLevelCompleted(LevelResult result) => LevelCompleted?.Invoke(result);

        public static void RaisePlayerHealthChanged(int current, int max) => PlayerHealthChanged?.Invoke(current, max);
        public static void RaisePlayerDied() => PlayerDied?.Invoke();
        public static void RaisePlayerFell() => PlayerFell?.Invoke();
        public static void RaiseAbilityActivated(string abilityId, float cooldown) => AbilityActivated?.Invoke(abilityId, cooldown);
        public static void RaiseAbilityStatusChanged(AbilityStatus status) => AbilityStatusChanged?.Invoke(status);
        public static void RaiseWeaponFired(string weaponId) => WeaponFired?.Invoke(weaponId);
        public static void RaiseWeaponHeatChanged(float heat, bool overheated) => WeaponHeatChanged?.Invoke(heat, overheated);
        public static void RaiseDamageDealt(DamageDealtInfo info) => DamageDealt?.Invoke(info);

        public static void RaiseEnemyDefeated(EnemyDefeatedInfo info) => EnemyDefeated?.Invoke(info);
        public static void RaiseItemCollected(ItemCollectedInfo info) => ItemCollected?.Invoke(info);
        public static void RaiseObjectiveUpdated(string description, int current, int target) => ObjectiveUpdated?.Invoke(description, current, target);
        public static void RaiseInteractionPromptChanged(string prompt) => InteractionPromptChanged?.Invoke(prompt);
        public static void RaiseInventoryChanged() => InventoryChanged?.Invoke();
        public static void RaiseSwitchActivated(string switchId, bool isOn) => SwitchActivated?.Invoke(switchId, isOn);
        public static void RaiseNoiseMade(Vector3 position, float radius) => NoiseMade?.Invoke(position, radius);

        public static void RaiseProgressSaved() => ProgressSaved?.Invoke();
        public static void RaiseSettingsChanged() => SettingsChanged?.Invoke();

        /// <summary>
        /// Clears every subscriber when Play Mode starts. Needed when "Enter Play Mode Options"
        /// skips domain reload, because static events would otherwise keep old listeners.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetAllSubscribers()
        {
            GameStateChanged = null;
            LevelLoadProgress = null;
            LevelLoadFailed = null;
            LevelStarted = null;
            LevelCompleted = null;
            PlayerHealthChanged = null;
            PlayerDied = null;
            PlayerFell = null;
            AbilityActivated = null;
            AbilityStatusChanged = null;
            WeaponFired = null;
            WeaponHeatChanged = null;
            DamageDealt = null;
            EnemyDefeated = null;
            ItemCollected = null;
            ObjectiveUpdated = null;
            InteractionPromptChanged = null;
            InventoryChanged = null;
            SwitchActivated = null;
            NoiseMade = null;
            ProgressSaved = null;
            SettingsChanged = null;
        }
    }
}
