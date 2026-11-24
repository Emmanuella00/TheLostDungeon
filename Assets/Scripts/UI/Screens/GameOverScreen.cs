using TheLostDungeon.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TheLostDungeon.UI
{
    /// <summary>Shown when the player dies: Retry the level or return to the Main Menu.</summary>
    public class GameOverScreen : UIScreen
    {
        private Button retryButton;
        private Text details;
        private Image backdrop;
        private float time;
        private Color backdropColour;

        protected override Selectable FirstSelected => retryButton;

        protected override void Build()
        {
            backdrop = UIBuilder.Backdrop(Root, new Color(0.25f, 0f, 0f, 0.6f));
            backdropColour = backdrop.color;   // keeps your prefab edit

            Image panel = UIBuilder.Panel(Root, "Panel", UITheme.PanelColour);
            UIBuilder.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640f, 520f));
            UIBuilder.Vertical(panel, 16f, TextAnchor.UpperCenter, 40);

            UIBuilder.Size(UIBuilder.Label(panel.transform, "Title", "YOU DIED", 72, UITheme.Danger, TextAnchor.MiddleCenter, FontStyle.Bold), 560f, 90f);
            details = UIBuilder.Size(UIBuilder.Label(panel.transform, "Details", "", 26, UITheme.TextMuted, TextAnchor.MiddleCenter), 560f, 80f);
            UIBuilder.Size(UIBuilder.Rect("Spacer", panel.transform), 10f, 20f);

            retryButton = UIBuilder.Button(panel.transform, "Retry", () => LevelManager.Instance.ReloadCurrentLevel());
            UIBuilder.Button(panel.transform, "Main Menu", () => UI.GoToMainMenu(), isBack: true);
        }

        protected override void OnShow()
        {
            time = 0f;
            if (!LevelManager.HasInstance || LevelManager.Instance.CurrentLevel == null) return;
            LevelManager levels = LevelManager.Instance;
            details.text = $"{levels.CurrentLevel.DisplayName}\nSurvived {UIBuilder.FormatTime(levels.CurrentLevelElapsed)}  ·  {UI.LevelKills} enemies defeated";
        }

        protected override void OnTick(float dt)
        {
            // Slow red pulse behind the panel.
            time += dt;
            Color c = backdropColour;
            c.r = Mathf.Clamp01(c.r + 0.08f * Mathf.Sin(time * 2f));
            backdrop.color = c;
        }
    }
}
