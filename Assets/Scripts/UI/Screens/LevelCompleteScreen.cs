using TheLostDungeon.Core;
using TheLostDungeon.Events;
using TheLostDungeon.Levels;
using TheLostDungeon.Online;
using UnityEngine;
using UnityEngine.UI;

namespace TheLostDungeon.UI
{
    /// <summary>
    /// Results after a level: stars (pop in one by one), time vs par, score, enemies defeated,
    /// a "new level unlocked" badge, then Next Level / Replay / Main Menu.
    /// Filled from GameEvents.LevelCompleted (raised just before the state changes).
    /// </summary>
    public class LevelCompleteScreen : UIScreen
    {
        private Text title, levelName, timeText, scoreText, killsText;
        private Image[] stars;
        private RectTransform unlockBadge;
        private Button nextButton, replayButton;
        private Text uploadText;
        private LevelResult result;
        private float starTimer;

        protected override Selectable FirstSelected => nextButton.gameObject.activeSelf ? nextButton : replayButton;

        protected override void Build()
        {
            UIBuilder.Backdrop(Root, UITheme.Dim);

            Image panel = UIBuilder.Panel(Root, "Panel", UITheme.PanelColour);
            UIBuilder.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 860f));
            UIBuilder.Vertical(panel, 14f, TextAnchor.UpperCenter, 40);

            title = UIBuilder.Size(UIBuilder.Label(panel.transform, "Title", "LEVEL COMPLETE", 56, UITheme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold), 680f, 70f);
            levelName = UIBuilder.Size(UIBuilder.Label(panel.transform, "Level", "", 28, UITheme.Text, TextAnchor.MiddleCenter), 680f, 40f);

            RectTransform starRow = UIBuilder.Rect("Stars", panel.transform);
            UIBuilder.Size(starRow, 680f, 130f);
            UIBuilder.Horizontal(starRow, 24f, TextAnchor.MiddleCenter);
            stars = new Image[3];
            for (int i = 0; i < 3; i++) stars[i] = UIBuilder.Size(UIBuilder.Icon(starRow, "Star_" + (i + 1), IconFactory.StarOutline, UITheme.Locked, 110f), 110f, 110f);

            timeText = StatRow(panel.transform, "Time");
            scoreText = StatRow(panel.transform, "Score");
            killsText = StatRow(panel.transform, "Enemies defeated");

            unlockBadge = UIBuilder.Rect("Unlocked", panel.transform);
            UIBuilder.Size(unlockBadge, 680f, 50f);
            UIBuilder.Horizontal(unlockBadge, 12f, TextAnchor.MiddleCenter);
            UIBuilder.Size(UIBuilder.Icon(unlockBadge, "Icon", IconFactory.Unlock, UITheme.Gold, 40f), 40f, 40f);
            UIBuilder.Size(UIBuilder.Label(unlockBadge, "Text", "Next level unlocked", 26, UITheme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold), 300f, 44f);

            uploadText = UIBuilder.Size(UIBuilder.Label(panel.transform, "Upload", "", 22, UITheme.TextMuted, TextAnchor.MiddleCenter), 680f, 30f);
            if (UIBuilder.IsNew(uploadText)) uploadText.transform.SetSiblingIndex(unlockBadge.GetSiblingIndex() + 1);

            UIBuilder.Size(UIBuilder.Rect("Spacer", panel.transform), 10f, 10f);
            nextButton = UIBuilder.Button(panel.transform, "Next Level", () => LevelManager.Instance.LoadNextLevel());
            replayButton = UIBuilder.Button(panel.transform, "Replay", () => LevelManager.Instance.ReloadCurrentLevel());
            UIBuilder.Button(panel.transform, "Main Menu", () => UI.GoToMainMenu(), isBack: true);
        }

        public override void Subscribe() => GameEvents.LevelCompleted += HandleCompleted;
        public override void Unsubscribe() => GameEvents.LevelCompleted -= HandleCompleted;

        private void HandleCompleted(LevelResult levelResult) => result = levelResult;

        protected override void OnShow()
        {
            LevelManager levels = LevelManager.HasInstance ? LevelManager.Instance : null;
            LevelDefinition level = levels != null ? levels.CurrentLevel : null;
            bool finalLevel = levels != null && !levels.HasNextLevel;

            title.text = finalLevel ? "DUNGEON ESCAPED!" : "LEVEL COMPLETE";
            levelName.text = level != null ? $"Level {result.LevelIndex + 1}  ·  {level.DisplayName}" : "";
            string par = level != null && level.ParTimeSeconds > 0f ? $"   (par {UIBuilder.FormatTime(level.ParTimeSeconds)})" : "";
            timeText.text = UIBuilder.FormatTime(result.TimeSeconds) + par;
            scoreText.text = result.Score.ToString();
            killsText.text = UI.LevelKills.ToString();

            unlockBadge.gameObject.SetActive(result.UnlockedNewLevel && !finalLevel);
            nextButton.gameObject.SetActive(!finalLevel);

            foreach (Image star in stars)
            {
                star.sprite = IconFactory.StarOutline;
                star.color = UITheme.Locked;
                star.rectTransform.localScale = Vector3.one;
            }
            starTimer = 0f;
            UpdateUploadText();
        }

        /// <summary>Leaderboard feedback: the score is sent to the REST API when the level completes.</summary>
        private void UpdateUploadText()
        {
            if (!LeaderboardManager.HasInstance) { uploadText.text = ""; return; }
            switch (LeaderboardManager.Instance.LastSubmit)
            {
                case LeaderboardManager.SubmitState.Sending: uploadText.text = "Uploading campaign score..."; break;
                case LeaderboardManager.SubmitState.Sent: uploadText.text = "Campaign score uploaded to the leaderboard"; break;
                case LeaderboardManager.SubmitState.Queued: uploadText.text = "Offline: score saved, it will upload later"; break;
                case LeaderboardManager.SubmitState.LocalOnly: uploadText.text = "Score saved to the local leaderboard"; break;
                default: uploadText.text = ""; break;
            }
        }

        protected override void OnTick(float dt)
        {
            // Stars pop in one after another (unscaled time: the game is frozen on this screen).
            starTimer += dt;
            UpdateUploadText();   // the upload finishes while this screen is open
            for (int i = 0; i < stars.Length; i++)
            {
                if (i >= result.Stars) continue;
                float t = starTimer - 0.4f - i * 0.35f;
                if (t < 0f) continue;
                if (stars[i].sprite != IconFactory.Star)
                {
                    stars[i].sprite = IconFactory.Star;
                    stars[i].color = UITheme.Gold;
                    UISounds.Click();
                }
                float pop = t < 0.25f ? 1f + 0.4f * Mathf.Sin(t / 0.25f * Mathf.PI) : 1f;
                stars[i].rectTransform.localScale = Vector3.one * pop;
            }
        }

        private static Text StatRow(Transform parent, string label)
        {
            RectTransform row = UIBuilder.Rect("Row_" + label, parent);
            UIBuilder.Size(row, 560f, 40f);
            UIBuilder.Horizontal(row, 10f, TextAnchor.MiddleCenter);
            UIBuilder.Size(UIBuilder.Label(row, "Label", label, 26, UITheme.TextMuted, TextAnchor.MiddleLeft), 280f, 40f);
            return UIBuilder.Size(UIBuilder.Label(row, "Value", "", 28, UITheme.Text, TextAnchor.MiddleRight, FontStyle.Bold), 270f, 40f);
        }
    }
}
