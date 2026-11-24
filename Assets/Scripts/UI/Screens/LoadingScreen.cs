using TheLostDungeon.Core;
using TheLostDungeon.Events;
using TheLostDungeon.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace TheLostDungeon.UI
{
    /// <summary>
    /// LOADING FEEDBACK for Addressables: level name, its objective, a progress bar driven by
    /// GameEvents.LevelLoadProgress and a rotating gameplay tip.
    /// </summary>
    public class LoadingScreen : UIScreen
    {
        private static readonly string[] Tips =
        {
            "Shielded Guardians block shots from the front. Break their guard with a melee bash.",
            "Your blaster overheats. Watch the heat bar under the crosshair.",
            "Gunshots and explosions make noise. Enemies nearby will come to investigate.",
            "Explosive barrels set each other off. Lure enemies into a chain reaction.",
            "Dash (Q) through danger, raise your Shield (R) when the bolts start flying.",
            "Faster clears earn more stars. Beat the par time for three stars."
        };

        private Text levelName;
        private Text objective;
        private Text percent;
        private Text tip;
        private Image fill;
        private Image spinner;
        private float shownProgress;
        private float targetProgress;

        /// <summary>Set by UIManager.GoToMainMenu so the screen says "Main Menu" instead of the last level.</summary>
        public bool LoadingMenu { get; set; }

        protected override void Build()
        {
            UIBuilder.Backdrop(Root, UITheme.Background);

            Text loading = UIBuilder.Label(Root, "Loading", "LOADING", 28, UITheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIBuilder.Place(loading.rectTransform, new Vector2(0f, 0f), new Vector2(140f, 300f), new Vector2(600f, 40f));

            levelName = UIBuilder.Label(Root, "LevelName", "", 72, UITheme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIBuilder.Place(levelName.rectTransform, new Vector2(0f, 0f), new Vector2(140f, 220f), new Vector2(1400f, 90f));

            objective = UIBuilder.Label(Root, "Objective", "", 26, UITheme.TextMuted, TextAnchor.MiddleLeft);
            UIBuilder.Place(objective.rectTransform, new Vector2(0f, 0f), new Vector2(140f, 170f), new Vector2(1400f, 40f));

            Image bar = UIBuilder.Bar(Root, "Progress", new Color(0.1f, 0.14f, 0.2f, 1f), UITheme.Accent, out fill);
            UIBuilder.Place(bar.rectTransform, new Vector2(0f, 0f), new Vector2(140f, 110f), new Vector2(1500f, 18f));

            percent = UIBuilder.Label(Root, "Percent", "0%", 26, UITheme.Text, TextAnchor.MiddleRight, FontStyle.Bold);
            UIBuilder.Place(percent.rectTransform, new Vector2(1f, 0f), new Vector2(-140f, 140f), new Vector2(200f, 40f));

            tip = UIBuilder.Label(Root, "Tip", "", 22, UITheme.TextMuted, TextAnchor.MiddleLeft, FontStyle.Italic);
            UIBuilder.Place(tip.rectTransform, new Vector2(0f, 0f), new Vector2(140f, 50f), new Vector2(1500f, 40f));

            spinner = UIBuilder.Icon(Root, "Spinner", IconFactory.Arc, UITheme.Accent, 64f);
            UIBuilder.Place(spinner.rectTransform, new Vector2(1f, 1f), new Vector2(-80f, -80f), new Vector2(64f, 64f));
        }

        public override void Subscribe() => GameEvents.LevelLoadProgress += HandleProgress;
        public override void Unsubscribe() => GameEvents.LevelLoadProgress -= HandleProgress;

        private void HandleProgress(float progress) => targetProgress = Mathf.Clamp01(progress);

        protected override void OnShow()
        {
            shownProgress = 0f;
            targetProgress = 0f;
            fill.fillAmount = 0f;
            tip.text = "TIP  " + Tips[Random.Range(0, Tips.Length)];

            LevelDefinition level = null;
            if (LoadingMenu)
            {
                levelName.text = "MAIN MENU";
            }
            else if (LevelManager.HasInstance)
            {
                int index = LevelManager.Instance.LastRequestedLevelIndex;
                if (index >= 0 && index < LevelManager.Instance.LevelCount) level = LevelManager.Instance.Levels[index];
                levelName.text = level != null ? $"{index + 1:00}  {level.DisplayName.ToUpperInvariant()}" : "MAIN MENU";
            }
            objective.text = level != null ? level.ObjectiveSummary : "";
        }

        protected override void OnTick(float dt)
        {
            shownProgress = Mathf.MoveTowards(shownProgress, targetProgress, dt * 2f);
            fill.fillAmount = shownProgress;
            percent.text = Mathf.RoundToInt(shownProgress * 100f) + "%";
            spinner.rectTransform.Rotate(0f, 0f, -240f * dt);
        }

        protected override void OnHide() => LoadingMenu = false;
    }
}
