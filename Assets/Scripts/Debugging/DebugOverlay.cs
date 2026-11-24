#if UNITY_EDITOR || DEVELOPMENT_BUILD
using TheLostDungeon.Abilities;
using TheLostDungeon.Combat;
using TheLostDungeon.Core;
using TheLostDungeon.Events;
using TheLostDungeon.Items;
using TheLostDungeon.Levels;
using TheLostDungeon.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TheLostDungeon.Debugging
{
    /// <summary>
    /// TEMPORARY test panel (Editor and Development builds only, never in release builds).
    /// Lets you test game states, level unlocking and saving before the real menus exist.
    ///
    /// Press F1 to show/hide it. While it's open the cursor is unlocked and player control
    /// is paused so you can click the buttons.
    ///
    /// It creates itself automatically, so there is nothing to add to any scene.
    /// Now that the real UI exists it starts HIDDEN, and its HUD preview / loading box are
    /// switched off. It stays as a cheat panel for testing (unlock abilities, jump to levels).
    /// </summary>
    public class DebugOverlay : MonoBehaviour
    {
        private bool visible;   // hidden by default now that the real UI exists
        private Vector2 scroll;
        private int playerHealth = -1;
        private int playerMaxHealth = -1;
        private float weaponHeat;
        private bool weaponOverheated;
        private string lastHit = "-";
        private string prompt;
        private string objective = "";
        private string loadError;
        private float loadProgress = -1f;

        private void OnEnable()
        {
            GameEvents.PlayerHealthChanged += HandleHealth;
            GameEvents.WeaponHeatChanged += HandleHeat;
            GameEvents.DamageDealt += HandleDamageDealt;
            GameEvents.InteractionPromptChanged += HandlePrompt;
            GameEvents.ObjectiveUpdated += HandleObjective;
            GameEvents.LevelStarted += HandleLevelStarted;
            GameEvents.LevelLoadFailed += HandleLoadFailed;
            GameEvents.LevelLoadProgress += HandleLoadProgress;
        }

        private void OnDisable()
        {
            GameEvents.PlayerHealthChanged -= HandleHealth;
            GameEvents.WeaponHeatChanged -= HandleHeat;
            GameEvents.DamageDealt -= HandleDamageDealt;
            GameEvents.InteractionPromptChanged -= HandlePrompt;
            GameEvents.ObjectiveUpdated -= HandleObjective;
            GameEvents.LevelStarted -= HandleLevelStarted;
            GameEvents.LevelLoadFailed -= HandleLoadFailed;
            GameEvents.LevelLoadProgress -= HandleLoadProgress;
        }

        private void HandleLoadFailed(string reason) { loadError = reason; loadProgress = -1f; }
        private void HandleLoadProgress(float progress) { loadProgress = progress >= 1f ? -1f : progress; if (progress <= 0f) loadError = null; }

        private void HandlePrompt(string text) => prompt = text;
        private void HandleObjective(string description, int current, int target) =>
            objective = target > 1 ? $"{description}  ({current}/{target})" : description;
        private void HandleLevelStarted(int levelIndex) { objective = ""; loadProgress = -1f; loadError = null; }

        /// <summary>Temporary loading bar and load-failure message (with Retry / Main Menu) until the real UI.</summary>
        private void DrawLoadingState()
        {
            var centre = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontSize = 15, wordWrap = true };
            if (loadProgress >= 0f)
            {
                GUI.Box(new Rect(Screen.width / 2 - 160, Screen.height - 70, 320, 30), $"Loading... {loadProgress * 100f:0}%", centre);
            }
            if (!string.IsNullOrEmpty(loadError) && LevelManager.HasInstance)
            {
                var area = new Rect(Screen.width / 2 - 220, Screen.height / 2 - 70, 440, 140);
                GUI.Box(area, "LOAD FAILED\n" + loadError, centre);
                if (GUI.Button(new Rect(area.x + 60, area.yMax - 40, 140, 30), "Retry"))
                {
                    loadError = null;
                    LevelManager.Instance.TryLoadLevel(LevelManager.Instance.LastRequestedLevelIndex);
                }
                if (GUI.Button(new Rect(area.x + 240, area.yMax - 40, 140, 30), "Main Menu"))
                {
                    loadError = null;
                    LevelManager.Instance.LoadMainMenu();
                }
            }
        }

        /// <summary>Temporary stand-in for the HUD: objective at the top, prompt under the crosshair.</summary>
        private void DrawHudPreview()
        {
            var centre = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16, richText = true };
            if (!string.IsNullOrEmpty(objective))
                GUI.Label(new Rect(0, 8, Screen.width, 30), $"<b>OBJECTIVE:</b> {objective}", centre);
            GUI.Label(new Rect(Screen.width / 2 - 10, Screen.height / 2 - 10, 20, 20), "+", centre);
            if (!string.IsNullOrEmpty(prompt))
                GUI.Label(new Rect(0, Screen.height / 2 + 30, Screen.width, 30), $"[E]  {prompt}", centre);
        }

        private void HandleHealth(int current, int max) { playerHealth = current; playerMaxHealth = max; }
        private void HandleHeat(float heat, bool overheated) { weaponHeat = heat; weaponOverheated = overheated; }
        private void HandleDamageDealt(DamageDealtInfo info) =>
            lastHit = $"{info.Amount:0} dmg{(info.Blocked ? " (BLOCKED by shield)" : "")}{(info.Killed ? " - KILL" : "")}";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            var go = new GameObject("DebugOverlay");
            DontDestroyOnLoad(go);
            go.AddComponent<DebugOverlay>();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
            {
                visible = !visible;
                bool playing = !GameManager.HasInstance || GameManager.Instance.IsPlaying;
                SetPlayerControl(!visible && playing);
            }

            // A newly loaded level's PlayerController locks the cursor on Start; undo that
            // while the panel is open so the buttons stay clickable.
            if (visible && Cursor.lockState == CursorLockMode.Locked)
            {
                SetPlayerControl(false);
            }
        }

        private void Start()
        {
            if (visible) SetPlayerControl(false);   // don't lock the cursor in menus when hidden
        }

        private void OnGUI()
        {
            if (!TheLostDungeon.UI.UIManager.HasInstance)
            {
                DrawHudPreview();
                DrawLoadingState();
            }
            if (!visible) return;
            if (!GameManager.HasInstance || !LevelManager.HasInstance || !SaveManager.HasInstance)
            {
                GUI.Label(new Rect(10, 10, 500, 25), "Systems not found. Run Tools > The Lost Dungeon > Setup Core Systems.");
                return;
            }

            GameManager game = GameManager.Instance;
            LevelManager levels = LevelManager.Instance;
            LevelProgression progression = levels.Progression;

            GUILayout.BeginArea(new Rect(10, 10, 330, Screen.height - 20), GUI.skin.box);
            scroll = GUILayout.BeginScrollView(scroll);

            GUILayout.Label("<b>DEBUG PANEL</b>  (F1 to hide)", Rich());
            GUILayout.Label($"Game state: <b>{game.CurrentState}</b>", Rich());
            GUILayout.Label($"Current level: {(levels.CurrentLevel != null ? levels.CurrentLevel.DisplayName : "none (menu)")}");
            if (levels.CurrentLevel != null) GUILayout.Label($"Time in level: {levels.CurrentLevelElapsed:0.0}s");
            GUILayout.Label($"Can continue: {game.CanContinue}");

            GUILayout.Space(8);
            GUILayout.Label("<b>Main menu actions</b>", Rich());
            if (GUILayout.Button("Start New Game (wipes save)")) game.StartNewGame();
            GUI.enabled = game.CanContinue;
            if (GUILayout.Button("Continue")) game.ContinueGame();
            GUI.enabled = true;
            if (GUILayout.Button("Go to Main Menu")) levels.LoadMainMenu();

            GUILayout.Space(8);
            GUILayout.Label("<b>Levels</b>", Rich());
            for (int i = 0; i < levels.LevelCount; i++)
            {
                LevelStatus status = progression.GetStatus(i);
                var record = progression.GetRecord(i);
                string extra = status == LevelStatus.Completed ? $"  ★{record.bestStars}  {record.bestTimeSeconds:0}s" : "";

                GUI.enabled = status != LevelStatus.Locked;
                if (GUILayout.Button($"{i + 1}. {levels.Levels[i].DisplayName}  [{status}]{extra}"))
                {
                    levels.TryLoadLevel(i);
                }
                GUI.enabled = true;
            }

            GUILayout.Space(8);
            if (IsInLevel(levels))
            {
                GUILayout.Label("<b>Combat</b>", Rich());
                Health playerHp = FindPlayerHealth();
                GUILayout.Label(playerHealth >= 0 ? $"Player health: {playerHealth}/{playerMaxHealth}" : "Player health: (no Player in scene)");
                GUILayout.Label($"Blaster heat: {weaponHeat * 100f:0}%{(weaponOverheated ? "  OVERHEATED" : "")}");
                GUILayout.Label($"Last hit: {lastHit}");
                PlayerInventory inv = playerHp != null ? playerHp.GetComponent<PlayerInventory>() : null;
                if (inv != null)
                {
                    string items = "";
                    foreach (var pair in inv.Items) items += $"{inv.DisplayNameOf(pair.Key)} x{pair.Value}   ";
                    GUILayout.Label($"Inventory: {(items.Length > 0 ? items : "empty")}");
                }
                GUI.enabled = playerHp != null && playerHp.IsAlive && game.IsPlaying;
                if (GUILayout.Button("Damage player 25"))
                    playerHp.TakeDamage(new DamageInfo(25f, DamageType.Melee, playerHp.transform.position, playerHp.transform.forward));
                if (GUILayout.Button("Heal player 50")) playerHp.Heal(50f);
                GUI.enabled = true;
                PlayerAbilities abilities = playerHp != null ? playerHp.GetComponent<PlayerAbilities>() : null;
                if (abilities != null)
                {
                    for (int i = 0; i < abilities.Abilities.Count; i++)
                    {
                        IAbility a = abilities.Abilities[i];
                        if (a == null) continue;
                        string state = !abilities.IsUnlocked(i) ? "LOCKED" : a.IsActive ? "ACTIVE" : a.IsReady ? "ready" : $"{a.CooldownRemaining:0.0}s";
                        GUILayout.Label($"Ability {(i == 0 ? "Q" : "R")}: {a.DisplayName} [{state}]");
                    }
                    if (GUILayout.Button("Unlock all abilities (saved)") && SaveManager.HasInstance)
                    {
                        foreach (IAbility a in abilities.Abilities)
                            if (a != null && !SaveManager.Instance.Data.unlockedAbilities.Contains(a.Id)) SaveManager.Instance.Data.unlockedAbilities.Add(a.Id);
                        SaveManager.Instance.Save();
                    }
                }
                GUILayout.Label("Hide panel (F1), then: Left click = shoot, Right click / F = melee, Q / R = abilities", Small());
                GUILayout.Space(8);
            }

            GUILayout.Label("<b>Simulate gameplay events</b>", Rich());
            bool inLevel = levels.CurrentLevel != null;
            GUI.enabled = inLevel && game.IsPlaying;
            if (GUILayout.Button("Win this level (score 100)")) levels.CompleteCurrentLevel(100);
            if (GUILayout.Button("Kill player")) GameEvents.RaisePlayerDied();
            if (GUILayout.Button("Defeat an enemy")) GameEvents.RaiseEnemyDefeated(new EnemyDefeatedInfo("debug_bot", Vector3.zero, 10));
            if (GUILayout.Button("Collect 50 credits")) GameEvents.RaiseItemCollected(new ItemCollectedInfo("credits", 50));
            GUI.enabled = inLevel && (game.CurrentState == GameState.Playing || game.CurrentState == GameState.Paused);
            if (GUILayout.Button(game.CurrentState == GameState.Paused ? "Resume" : "Pause")) game.TogglePause();
            GUI.enabled = inLevel && (game.CurrentState == GameState.GameOver || game.CurrentState == GameState.LevelComplete);
            if (GUILayout.Button("Retry level")) levels.ReloadCurrentLevel();
            if (GUILayout.Button("Next level")) levels.LoadNextLevel();
            GUI.enabled = true;

            GUILayout.Space(8);
            var stats = SaveManager.Instance.Data.stats;
            GUILayout.Label("<b>Saved stats</b>", Rich());
            GUILayout.Label($"Enemies defeated: {stats.enemiesDefeated}   Deaths: {stats.deaths}");
            GUILayout.Label($"Credits: {stats.credits}   Play time: {stats.totalPlayTimeSeconds:0}s");
            if (GUILayout.Button("Save now")) SaveManager.Instance.Save();
            GUILayout.Label($"Save file folder:\n{Application.persistentDataPath}", Small());

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private static bool IsInLevel(LevelManager levels) => levels.CurrentLevel != null;

        private static Health FindPlayerHealth()
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            return player != null ? player.GetComponent<Health>() : null;
        }

        private static void SetPlayerControl(bool enabled)
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                player.SetControlEnabled(enabled);
            }
            else
            {
                Cursor.lockState = enabled ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = !enabled;
            }
        }

        private static GUIStyle Rich() => new GUIStyle(GUI.skin.label) { richText = true };
        private static GUIStyle Small() => new GUIStyle(GUI.skin.label) { fontSize = 10, wordWrap = true };
    }
}
#endif
