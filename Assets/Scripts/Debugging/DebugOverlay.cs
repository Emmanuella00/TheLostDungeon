#if UNITY_EDITOR || DEVELOPMENT_BUILD
using TheLostDungeon.Core;
using TheLostDungeon.Events;
using TheLostDungeon.Levels;
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
    /// Delete this file once the real UI is finished.
    /// </summary>
    public class DebugOverlay : MonoBehaviour
    {
        private bool visible = true;
        private Vector2 scroll;

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
                SetPlayerControl(!visible);
            }

            // A newly loaded level's PlayerController locks the cursor on Start; undo that
            // while the panel is open so the buttons stay clickable.
            if (visible && Cursor.lockState == CursorLockMode.Locked)
            {
                SetPlayerControl(false);
            }
        }

        private void Start() => SetPlayerControl(!visible);

        private void OnGUI()
        {
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
