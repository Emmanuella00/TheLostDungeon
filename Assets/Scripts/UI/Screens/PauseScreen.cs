using TheLostDungeon.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TheLostDungeon.UI
{
    /// <summary>Pause menu (Esc / Start): Resume, Restart, Settings, Main Menu. Game time is frozen.</summary>
    public class PauseScreen : UIScreen
    {
        private Button resumeButton;
        private Text levelText;

        protected override Selectable FirstSelected => resumeButton;

        protected override void Build()
        {
            UIBuilder.Backdrop(Root, UITheme.Dim);

            Image panel = UIBuilder.Panel(Root, "Panel", UITheme.PanelColour);
            UIBuilder.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 620f));
            UIBuilder.Vertical(panel, 16f, TextAnchor.UpperCenter, 40);

            UIBuilder.Size(UIBuilder.Label(panel.transform, "Title", "PAUSED", 56, UITheme.Text, TextAnchor.MiddleCenter, FontStyle.Bold), 480f, 70f);
            levelText = UIBuilder.Size(UIBuilder.Label(panel.transform, "Level", "", 24, UITheme.TextMuted, TextAnchor.MiddleCenter), 480f, 36f);
            UIBuilder.Size(UIBuilder.Rect("Spacer", panel.transform), 10f, 16f);

            resumeButton = UIBuilder.Button(panel.transform, "Resume", () => GameManager.Instance.TogglePause());
            UIBuilder.Button(panel.transform, "Restart Level", () => LevelManager.Instance.ReloadCurrentLevel());
            UIBuilder.Button(panel.transform, "Settings", () => UI.OpenSettings(this));
            UIBuilder.Button(panel.transform, "Main Menu", () => UI.GoToMainMenu(), isBack: true);
        }

        protected override void OnShow()
        {
            if (!LevelManager.HasInstance || LevelManager.Instance.CurrentLevel == null) return;
            LevelManager levels = LevelManager.Instance;
            levelText.text = $"Level {levels.CurrentLevelIndex + 1}  ·  {levels.CurrentLevel.DisplayName}  ·  {UIBuilder.FormatTime(levels.CurrentLevelElapsed)}";
        }
    }
}
