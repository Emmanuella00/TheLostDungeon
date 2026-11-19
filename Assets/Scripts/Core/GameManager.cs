using TheLostDungeon.Data;
using TheLostDungeon.Events;
using UnityEngine;

namespace TheLostDungeon.Core
{
    /// <summary>
    /// Owns the overall game state (menu, playing, paused, game over...) and the top-level
    /// actions the main menu calls: Start Game, Continue, Pause, Quit.
    /// Every state change is broadcast through GameEvents.GameStateChanged, which is how the
    /// pause menu, HUD, cursor and music react without the GameManager referencing them.
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        public GameState CurrentState { get; private set; } = GameState.Boot;
        public bool IsPlaying => CurrentState == GameState.Playing;

        /// <summary>True when there is saved progress to continue from.</summary>
        public bool CanContinue => SaveManager.Instance.HasSaveFile && LevelManager.Instance.Progression.HasAnyProgress;

        protected override void OnSingletonAwake()
        {
            GameSettings.ApplyAll();
        }

        private void OnEnable()
        {
            GameEvents.PlayerDied += HandlePlayerDied;
            GameEvents.LevelCompleted += HandleLevelCompleted;
        }

        private void OnDisable()
        {
            GameEvents.PlayerDied -= HandlePlayerDied;
            GameEvents.LevelCompleted -= HandleLevelCompleted;
        }

        public void ChangeState(GameState newState)
        {
            if (newState == CurrentState) return;

            GameState previous = CurrentState;
            CurrentState = newState;

            // Freeze gameplay on overlay screens; UI keeps working because it uses unscaled time.
            Time.timeScale = newState == GameState.Paused
                             || newState == GameState.GameOver
                             || newState == GameState.LevelComplete ? 0f : 1f;

            GameEvents.RaiseGameStateChanged(previous, newState);
        }

        public void TogglePause()
        {
            if (CurrentState == GameState.Playing) ChangeState(GameState.Paused);
            else if (CurrentState == GameState.Paused) ChangeState(GameState.Playing);
        }

        /// <summary>Main menu "Start Game": wipes progress and starts level 1.</summary>
        public void StartNewGame()
        {
            SaveManager.Instance.ResetProgress();
            LevelManager.Instance.TryLoadLevel(0);
        }

        /// <summary>Main menu "Continue": jumps to the next unfinished level.</summary>
        public void ContinueGame()
        {
            LevelManager.Instance.TryLoadLevel(LevelManager.Instance.Progression.NextLevelToPlay);
        }

        /// <summary>
        /// Main menu "Exit". CONDITIONAL COMPILATION: browsers can't close the game, so on
        /// WebGL the Exit button should be hidden (see ShowExitButton) and this does nothing.
        /// </summary>
        public void QuitGame()
        {
            SaveManager.Instance.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
            Debug.Log("[GameManager] Quit is not supported in the browser.");
#else
            Application.Quit();
#endif
        }

        /// <summary>The main menu uses this to hide the Exit button on WebGL.</summary>
        public static bool ShowExitButton
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return false;
#else
                return true;
#endif
            }
        }

        private void HandlePlayerDied() => ChangeState(GameState.GameOver);

        private void HandleLevelCompleted(LevelResult result) => ChangeState(GameState.LevelComplete);
    }
}
