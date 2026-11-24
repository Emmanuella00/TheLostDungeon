using TheLostDungeon.Data;
using UnityEngine;
using UnityEngine.UI;

namespace TheLostDungeon.UI
{
    /// <summary>
    /// Volume (master / music / effects), look sensitivity, graphics quality and (desktop only)
    /// fullscreen. Values are stored by GameSettings in PlayerPrefs, which raises SettingsChanged
    /// so the AudioManager and player update live.
    /// </summary>
    public class SettingsScreen : UIScreen
    {
        public UIScreen ReturnTo { get; set; }

        private Slider master, music, sfx, sensitivity;
        private Text masterValue, musicValue, sfxValue, sensitivityValue, qualityValue;
#if UNITY_STANDALONE
        private Button fullscreenButton;
#endif

        protected override Selectable FirstSelected => master;

        protected override void Build()
        {
            UIBuilder.Backdrop(Root, UITheme.Dim);

            Image panel = UIBuilder.Panel(Root, "Panel", UITheme.PanelColour);
            UIBuilder.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 760f));
            UIBuilder.Vertical(panel, 18f, TextAnchor.UpperCenter, 40);

            UIBuilder.Size(UIBuilder.Label(panel.transform, "Title", "SETTINGS", 52, UITheme.Text, TextAnchor.MiddleCenter, FontStyle.Bold), 800f, 70f);

            master = SliderRow(panel.transform, "Master volume", 0f, 1f, GameSettings.MasterVolume, v => { GameSettings.MasterVolume = v; Refresh(); }, out masterValue);
            music = SliderRow(panel.transform, "Music", 0f, 1f, GameSettings.MusicVolume, v => { GameSettings.MusicVolume = v; Refresh(); }, out musicValue);
            sfx = SliderRow(panel.transform, "Effects", 0f, 1f, GameSettings.SfxVolume, v => { GameSettings.SfxVolume = v; Refresh(); }, out sfxValue);
            sensitivity = SliderRow(panel.transform, "Look sensitivity", 0.1f, 3f, GameSettings.LookSensitivity, v => { GameSettings.LookSensitivity = v; Refresh(); }, out sensitivityValue);

            RectTransform qualityRow = Row(panel.transform, "Graphics quality");
            UIBuilder.Button(qualityRow, "<", () => ChangeQuality(-1), 64f, 48f);
            qualityValue = UIBuilder.Size(UIBuilder.Label(qualityRow, "Value", "", 26, UITheme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold), 228f, 48f);
            UIBuilder.Button(qualityRow, ">", () => ChangeQuality(1), 64f, 48f);

#if UNITY_STANDALONE
            // Fullscreen only makes sense on desktop builds (CONDITIONAL COMPILATION).
            RectTransform fullscreenRow = Row(panel.transform, "Fullscreen");
            fullscreenButton = UIBuilder.Button(fullscreenRow, "On", ToggleFullscreen, 228f, 48f);
#endif

            UIBuilder.Size(UIBuilder.Rect("Spacer", panel.transform), 10f, 20f);
            UIBuilder.Button(panel.transform, "Back", () => UI.CloseSettings(), 300f, 64f, isBack: true);
        }

        protected override void OnShow()
        {
            master.SetValueWithoutNotify(GameSettings.MasterVolume);
            music.SetValueWithoutNotify(GameSettings.MusicVolume);
            sfx.SetValueWithoutNotify(GameSettings.SfxVolume);
            sensitivity.SetValueWithoutNotify(GameSettings.LookSensitivity);
            Refresh();
        }

        private void Refresh()
        {
            masterValue.text = Percent(GameSettings.MasterVolume);
            musicValue.text = Percent(GameSettings.MusicVolume);
            sfxValue.text = Percent(GameSettings.SfxVolume);
            sensitivityValue.text = GameSettings.LookSensitivity.ToString("0.0") + "x";

            string[] names = QualitySettings.names;
            int level = GameSettings.QualityLevel;
            qualityValue.text = level >= 0 && level < names.Length ? names[level] : "-";
#if UNITY_STANDALONE
            UIBuilder.SetButtonText(fullscreenButton, Screen.fullScreen ? "On" : "Off");
#endif
        }

        private void ChangeQuality(int direction)
        {
            int count = QualitySettings.names.Length;
            if (count == 0) return;
            GameSettings.QualityLevel = (GameSettings.QualityLevel + direction + count) % count;
            Refresh();
        }

#if UNITY_STANDALONE
        private void ToggleFullscreen()
        {
            bool fullscreen = !Screen.fullScreen;   // the change itself applies next frame
            Screen.fullScreen = fullscreen;
            UIBuilder.SetButtonText(fullscreenButton, fullscreen ? "On" : "Off");
        }
#endif

        private static RectTransform Row(Transform parent, string label)
        {
            RectTransform row = UIBuilder.Rect("Row_" + label, parent);
            UIBuilder.Size(row, 800f, 52f);
            UIBuilder.Horizontal(row, 12f, TextAnchor.MiddleLeft);
            UIBuilder.Size(UIBuilder.Label(row, "Label", label, 26, UITheme.Text, TextAnchor.MiddleLeft), 300f, 52f);
            return row;
        }

        private static Slider SliderRow(Transform parent, string label, float min, float max, float value,
                                        UnityEngine.Events.UnityAction<float> onChanged, out Text valueText)
        {
            RectTransform row = Row(parent, label);
            Slider slider = UIBuilder.Slider(row, "Slider", min, max, value, onChanged, 360f);
            valueText = UIBuilder.Size(UIBuilder.Label(row, "Value", "", 24, UITheme.Accent, TextAnchor.MiddleRight), 100f, 52f);
            return slider;
        }

        private static string Percent(float value) => Mathf.RoundToInt(value * 100f) + "%";
    }
}
