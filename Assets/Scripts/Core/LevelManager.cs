using System.Collections;
using System.Collections.Generic;
using TheLostDungeon.Content;
using TheLostDungeon.Events;
using TheLostDungeon.Levels;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TheLostDungeon.Core
{
    /// <summary>
    /// Owns the list of levels, enforces locking, loads scenes with progress feedback and records
    /// completions. UI screens call TryLoadLevel / LoadMainMenu; they never load scenes directly.
    ///
    /// Level scenes are loaded through ISceneContentLoader (Addressables): each level's content is
    /// loaded when needed and released when the player leaves. The main menu is a normal built-in
    /// scene so the game always has somewhere safe to return to.
    /// </summary>
    public class LevelManager : Singleton<LevelManager>
    {
        [SerializeField] private LevelDefinition[] levels = new LevelDefinition[0];
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private readonly ISceneContentLoader contentLoader = new AddressablesSceneLoader();
        private float levelStartTime;
        private Coroutine loadRoutine;

        public IReadOnlyList<LevelDefinition> Levels => levels;
        public int LevelCount => levels.Length;
        public int CurrentLevelIndex { get; private set; } = -1;
        public LevelDefinition CurrentLevel => IsValidIndex(CurrentLevelIndex) ? levels[CurrentLevelIndex] : null;
        public bool IsLoading => loadRoutine != null;
        public bool HasNextLevel => CurrentLevelIndex >= 0 && CurrentLevelIndex + 1 < levels.Length;
        public float CurrentLevelElapsed => CurrentLevelIndex >= 0 ? Time.time - levelStartTime : 0f;
        /// <summary>The level most recently requested (used by the "Retry" button after a failed load).</summary>
        public int LastRequestedLevelIndex { get; private set; } = -1;

        /// <summary>Progression rules over the current save. Cheap to create, always up to date.</summary>
        public LevelProgression Progression => new LevelProgression(SaveManager.Instance.Data, Mathf.Max(1, levels.Length));

        private void Start()
        {
            // When you press Play inside a level scene in the Editor, recognise it so the
            // game behaves as if that level had been loaded from the menu.
            string activeScene = SceneManager.GetActiveScene().name;
            int index = IndexOfScene(activeScene);

            if (index >= 0)
            {
                BeginLevel(index);
            }
            else
            {
                GameManager.Instance.ChangeState(GameState.MainMenu);
            }
        }

        public bool TryLoadLevel(int levelIndex)
        {
            if (IsLoading || !IsValidIndex(levelIndex)) return false;

            if (!Progression.IsUnlocked(levelIndex))
            {
                Debug.LogWarning($"[LevelManager] Level {levelIndex + 1} is locked.");
                return false;
            }

            LastRequestedLevelIndex = levelIndex;
            loadRoutine = levels[levelIndex].HasAddressableScene
                ? StartCoroutine(LoadAddressableLevelRoutine(levelIndex))
                : StartCoroutine(LoadSceneRoutine(levels[levelIndex].SceneName, levelIndex));   // fallback before setup
            return true;
        }

        public void ReloadCurrentLevel()
        {
            if (CurrentLevelIndex >= 0) TryLoadLevel(CurrentLevelIndex);
        }

        public bool LoadNextLevel() => HasNextLevel && TryLoadLevel(CurrentLevelIndex + 1);

        public void LoadMainMenu()
        {
            if (IsLoading) return;
            loadRoutine = StartCoroutine(LoadSceneRoutine(mainMenuSceneName, -1));
        }

        /// <summary>
        /// Called by the level's objective system when the win condition is met.
        /// Records the result, saves, then announces it to every listener.
        /// </summary>
        public void CompleteCurrentLevel(int score)
        {
            if (CurrentLevel == null) return;

            float time = CurrentLevelElapsed;
            int stars = LevelProgression.CalculateStars(time, CurrentLevel.ParTimeSeconds);

            Progression.CompleteLevel(CurrentLevelIndex, time, stars, score, out bool unlockedNewLevel);
            SaveManager.Instance.Save();

            GameEvents.RaiseLevelCompleted(new LevelResult(CurrentLevelIndex, time, score, stars, unlockedNewLevel));
        }

        private IEnumerator LoadAddressableLevelRoutine(int levelIndex)
        {
            GameManager.Instance.ChangeState(GameState.Loading);
            GameEvents.RaiseLevelLoadProgress(0f);

            string failure = null;
            yield return contentLoader.LoadLevel(levels[levelIndex], GameEvents.RaiseLevelLoadProgress, reason => failure = reason);
            loadRoutine = null;

            if (failure != null)
            {
                Debug.LogError($"[LevelManager] {failure}");
                GameEvents.RaiseLevelLoadFailed(failure);
                GameManager.Instance.ChangeState(CurrentLevelIndex >= 0 ? GameState.Playing : GameState.MainMenu);
                yield break;
            }

            BeginLevel(levelIndex);
        }

        private IEnumerator LoadSceneRoutine(string sceneName, int levelIndex)
        {
            GameManager.Instance.ChangeState(GameState.Loading);
            GameEvents.RaiseLevelLoadProgress(0f);

            // Fail gracefully instead of freezing if a scene is missing from Build Settings.
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                string reason = $"Scene '{sceneName}' isn't in File > Build Profiles > Scene List.";
                Debug.LogError($"[LevelManager] {reason}");
                GameEvents.RaiseLevelLoadFailed(reason);
                loadRoutine = null;
                GameManager.Instance.ChangeState(CurrentLevelIndex >= 0 ? GameState.Playing : GameState.MainMenu);
                yield break;
            }

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            while (!operation.isDone)
            {
                // Unity reports 0..0.9 while loading; 0.9..1 is activation.
                GameEvents.RaiseLevelLoadProgress(Mathf.Clamp01(operation.progress / 0.9f));
                yield return null;
            }
            GameEvents.RaiseLevelLoadProgress(1f);
            loadRoutine = null;

            if (levelIndex >= 0)
            {
                BeginLevel(levelIndex);
            }
            else
            {
                CurrentLevelIndex = -1;
                contentLoader.ReleaseCurrentLevel();   // the level's bundles are no longer needed
                GameManager.Instance.ChangeState(GameState.MainMenu);
            }
        }

        private void BeginLevel(int levelIndex)
        {
            CurrentLevelIndex = levelIndex;
            levelStartTime = Time.time;
            GameManager.Instance.ChangeState(GameState.Playing);
            GameEvents.RaiseLevelStarted(levelIndex);
        }

        private int IndexOfScene(string sceneName)
        {
            for (int i = 0; i < levels.Length; i++)
            {
                if (levels[i] != null && levels[i].SceneName == sceneName) return i;
            }
            return -1;
        }

        private bool IsValidIndex(int index) => index >= 0 && index < levels.Length && levels[index] != null;
    }
}
