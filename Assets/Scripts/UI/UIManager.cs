using System.Collections.Generic;
using TheLostDungeon.Core;
using TheLostDungeon.Events;
using TheLostDungeon.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TheLostDungeon.UI
{
    /// <summary>
    /// Owns the whole game UI: one persistent canvas with every screen (main menu, level select,
    /// settings, loading, HUD, pause, level complete, game over, load-failure message).
    ///
    /// It is driven by GameEvents.GameStateChanged (OBSERVER): the GameManager never calls the UI,
    /// it just changes state and the UI shows the matching screens. Buttons call the managers'
    /// public methods (StartNewGame, TryLoadLevel, TogglePause...).
    ///
    /// Created automatically before the first scene, like the Systems prefab, so every scene
    /// (menu or level) has the UI without placing anything.
    ///
    /// EDITABLE LAYOUT: Tools > The Lost Dungeon > Bake UI Prefab saves the whole UI as
    /// Assets/Resources/GameUI.prefab. If that prefab exists it is used, and everything you change
    /// in it (positions, sizes, colours, fonts, texts, icons) is kept: the screens only attach
    /// behaviour to the elements they find by name. Without the prefab the UI is generated.
    /// </summary>
    public class UIManager : Singleton<UIManager>
    {
        public const string PrefabName = "GameUI";

        private readonly List<UIScreen> screens = new List<UIScreen>();
        private EventSystem eventSystem;

        public MainMenuScreen MainMenu { get; private set; }
        public LevelSelectScreen LevelSelect { get; private set; }
        public SettingsScreen Settings { get; private set; }
        public LoadingScreen Loading { get; private set; }
        public HudScreen Hud { get; private set; }
        public PauseScreen Pause { get; private set; }
        public LevelCompleteScreen LevelComplete { get; private set; }
        public GameOverScreen GameOver { get; private set; }
        public MessageScreen Message { get; private set; }

        /// <summary>Enemies defeated since the current level started (shown on the result screens).</summary>
        public int LevelKills { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateUI()
        {
            if (HasInstance) return;
            GameObject prefab = Resources.Load<GameObject>(PrefabName);
            GameObject root = prefab != null ? Instantiate(prefab) : new GameObject("UI");
            root.name = "UI";
            root.AddComponent<UIManager>();
        }

        protected override void OnSingletonAwake()
        {
            ConfigureCanvas(gameObject);
            CreateEventSystem();

            screens.AddRange(CreateScreens(this, transform));
            MainMenu = Find<MainMenuScreen>();
            LevelSelect = Find<LevelSelectScreen>();
            Settings = Find<SettingsScreen>();
            Hud = Find<HudScreen>();
            Pause = Find<PauseScreen>();
            LevelComplete = Find<LevelCompleteScreen>();
            GameOver = Find<GameOverScreen>();
            Loading = Find<LoadingScreen>();
            Message = Find<MessageScreen>();

            foreach (UIScreen screen in screens) screen.Subscribe();
        }

        private void OnEnable()
        {
            GameEvents.GameStateChanged += HandleStateChanged;
            GameEvents.LevelLoadFailed += HandleLoadFailed;
            GameEvents.LevelStarted += HandleLevelStarted;
            GameEvents.EnemyDefeated += HandleEnemyDefeated;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            GameEvents.GameStateChanged -= HandleStateChanged;
            GameEvents.LevelLoadFailed -= HandleLoadFailed;
            GameEvents.LevelStarted -= HandleLevelStarted;
            GameEvents.EnemyDefeated -= HandleEnemyDefeated;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        protected override void OnDestroy()
        {
            if (Instance == this)
            {
                foreach (UIScreen screen in screens) screen.Unsubscribe();
            }
            base.OnDestroy();
        }

        private void Start()
        {
            GameState state = GameManager.HasInstance ? GameManager.Instance.CurrentState : GameState.MainMenu;
            ApplyState(state);
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            foreach (UIScreen screen in screens) screen.Tick(dt);
        }

        // ---------------------------------------------------------------- navigation

        public void ShowMainMenu() => ShowOnly(MainMenu);

        /// <summary>Leaves the level and loads the menu scene (used by pause, results and popups).</summary>
        public void GoToMainMenu()
        {
            if (!LevelManager.HasInstance) return;
            if (LevelManager.Instance.CurrentLevelIndex < 0)
            {
                // Already in the menu scene (e.g. a load failed from the menu): just show it.
                if (GameManager.HasInstance) GameManager.Instance.ChangeState(GameState.MainMenu);
                ShowOnly(MainMenu);
                return;
            }
            Loading.LoadingMenu = true;
            LevelManager.Instance.LoadMainMenu();
        }

        public void OpenLevelSelect() => ShowOnly(LevelSelect);

        /// <summary>Opens Settings; Back returns to the screen it was opened from.</summary>
        public void OpenSettings(UIScreen returnTo)
        {
            Settings.ReturnTo = returnTo;
            if (returnTo == Pause) ShowOnly(Hud, Settings);
            else ShowOnly(Settings);
        }

        public void CloseSettings()
        {
            UIScreen target = Settings.ReturnTo;
            if (target == Pause && GameManager.HasInstance && GameManager.Instance.CurrentState == GameState.Paused)
                ShowOnly(Hud, Pause);
            else
                ShowOnly(MainMenu);
        }

        private void ApplyState(GameState state)
        {
            switch (state)
            {
                case GameState.Boot:
                case GameState.MainMenu:
                    ShowOnly(MainMenu);
                    break;
                case GameState.Loading:
                    ShowOnly(Loading);
                    break;
                case GameState.Playing:
                    ShowOnly(Hud);
                    break;
                case GameState.Paused:
                    ShowOnly(Hud, Pause);
                    break;
                case GameState.LevelComplete:
                    ShowOnly(LevelComplete);
                    break;
                case GameState.GameOver:
                    ShowOnly(Hud, GameOver);
                    break;
            }
        }

        /// <summary>Shows exactly these screens (in this order, last on top). The message box is left alone.</summary>
        private void ShowOnly(params UIScreen[] visible)
        {
            foreach (UIScreen screen in screens)
            {
                if (screen == Message) continue;
                if (System.Array.IndexOf(visible, screen) < 0) screen.Hide();
            }
            foreach (UIScreen screen in visible) screen.Show();
            if (Message.IsVisible) Message.BringToFront();
        }

        // ---------------------------------------------------------------- events

        private void HandleStateChanged(GameState previous, GameState current) => ApplyState(current);

        private void HandleLevelStarted(int levelIndex) => LevelKills = 0;

        private void HandleEnemyDefeated(EnemyDefeatedInfo info) => LevelKills++;

        private void HandleLoadFailed(string reason)
        {
            ReleasePlayerControl();
            Message.ShowLoadFailure(reason);
        }

        /// <summary>Unlocks the cursor so a popup can be clicked even mid-level.</summary>
        public static void ReleasePlayerControl()
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null) player.SetControlEnabled(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // ---------------------------------------------------------------- setup

        /// <summary>
        /// Creates (or binds to) every screen, in drawing order. Used at runtime and by the
        /// editor's Bake UI Prefab tool (with ui = null), so both produce the same hierarchy.
        /// </summary>
        public static List<UIScreen> CreateScreens(UIManager ui, Transform parent)
        {
            var created = new List<UIScreen>
            {
                new MainMenuScreen(),
                new LevelSelectScreen(),
                new SettingsScreen(),
                new HudScreen(),
                new PauseScreen(),
                new LevelCompleteScreen(),
                new GameOverScreen(),
                new LoadingScreen(),
                new MessageScreen()
            };
            foreach (UIScreen screen in created) screen.Create(ui, parent);
            return created;
        }

        private T Find<T>() where T : UIScreen
        {
            foreach (UIScreen screen in screens)
            {
                if (screen is T match) return match;
            }
            return null;
        }

        /// <summary>Canvas settings (kept if the prefab already has them).</summary>
        public static void ConfigureCanvas(GameObject root)
        {
            root.layer = 5; // UI
            Canvas canvas = root.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = root.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
            }

            if (root.GetComponent<CanvasScaler>() == null)
            {
                var scaler = root.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            UIBuilder.GetOrAdd<GraphicRaycaster>(root);
        }

        private void CreateEventSystem()
        {
            // One EventSystem for the whole game, using the new Input System (mouse, keyboard, gamepad, touch).
            var esObject = new GameObject("EventSystem");
            esObject.transform.SetParent(transform, false);
            eventSystem = esObject.AddComponent<EventSystem>();
            var module = esObject.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        /// <summary>Scenes may contain their own EventSystem; two at once break UI input, so remove extras.</summary>
        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            foreach (EventSystem other in FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (other != eventSystem) Destroy(other.gameObject);
            }
            if (eventSystem != null) EventSystem.current = eventSystem;
        }
    }
}
