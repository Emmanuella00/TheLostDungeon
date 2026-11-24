using TheLostDungeon.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TheLostDungeon.UI
{
    /// <summary>Title screen: Start, Continue, Level Select, Settings, Exit (hidden on WebGL).</summary>
    public class MainMenuScreen : UIScreen
    {
        private Button startButton;
        private Button continueButton;
        private Button exitButton;

        protected override Selectable FirstSelected => continueButton != null && continueButton.interactable ? continueButton : startButton;

        protected override void Build()
        {
            Image background = UIBuilder.Backdrop(Root, UITheme.Background);
            Image accent = UIBuilder.Image(background.transform, "AccentLine", UITheme.Accent);
            UIBuilder.Place(accent.rectTransform, new Vector2(0f, 0.5f), new Vector2(130f, 0f), new Vector2(6f, 560f));
            Image vignette = UIBuilder.Image(background.transform, "Vignette", new Color(0f, 0f, 0f, 0.85f), IconFactory.Vignette);
            UIBuilder.Stretch(vignette.rectTransform);

            RectTransform column = UIBuilder.Place(UIBuilder.Rect("Column", Root), new Vector2(0f, 0.5f), new Vector2(160f, 0f), new Vector2(760f, 860f));
            UIBuilder.Vertical(column, 14f, TextAnchor.MiddleLeft);

            UIBuilder.Size(UIBuilder.Label(column, "Kicker", "CHRONICLES OF THE", 30, UITheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold), 760f, 40f);
            UIBuilder.Size(UIBuilder.Label(column, "Title", "LOST DUNGEON", 96, UITheme.Text, TextAnchor.MiddleLeft, FontStyle.Bold), 760f, 110f);
            UIBuilder.Size(UIBuilder.Label(column, "Subtitle", "Fight through five sealed sectors of the warehouse vault.", 24, UITheme.TextMuted, TextAnchor.MiddleLeft), 760f, 40f);
            UIBuilder.Size(UIBuilder.Rect("Spacer", column), 10f, 30f);

            startButton = UIBuilder.Button(column, "Start New Game", StartNewGame);
            continueButton = UIBuilder.Button(column, "Continue", () => GameManager.Instance.ContinueGame());
            UIBuilder.Button(column, "Level Select", () => UI.OpenLevelSelect());
            UIBuilder.Button(column, "Settings", () => UI.OpenSettings(this));
            exitButton = UIBuilder.Button(column, "Exit", () => GameManager.Instance.QuitGame(), isBack: true);

            Text platform = UIBuilder.Label(Root, "Platform", PlatformLabel(), 20, UITheme.TextMuted, TextAnchor.LowerRight);
            UIBuilder.Place(platform.rectTransform, new Vector2(1f, 0f), new Vector2(-30f, 24f), new Vector2(600f, 40f));
            platform.text = PlatformLabel();   // always set: depends on the build platform
        }

        protected override void OnShow()
        {
            bool canContinue = GameManager.HasInstance && GameManager.Instance.CanContinue;
            UIBuilder.SetInteractable(continueButton, canContinue);

            // CONDITIONAL COMPILATION (see GameManager.ShowExitButton): no Exit in the browser.
            exitButton.gameObject.SetActive(GameManager.ShowExitButton);
        }

        private void StartNewGame()
        {
            if (GameManager.HasInstance) GameManager.Instance.StartNewGame();
        }

        private static string PlatformLabel()
        {
#if UNITY_EDITOR
            return "Editor build";
#elif UNITY_WEBGL
            return "WebGL build";
#elif UNITY_ANDROID
            return "Android build";
#elif UNITY_IOS
            return "iOS build";
#elif UNITY_STANDALONE_WIN
            return "Windows build";
#else
            return "Desktop build";
#endif
        }
    }
}
