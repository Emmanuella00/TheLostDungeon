using System;
using TheLostDungeon.Events;
using UnityEngine;

namespace TheLostDungeon.Data
{
    /// <summary>
    /// Player settings stored in PlayerPrefs (small key-value device preferences), separate
    /// from the JSON progress save. Changing a value raises GameEvents.SettingsChanged so the
    /// AudioManager, camera, etc. can update without the settings menu knowing about them.
    /// </summary>
    public static class GameSettings
    {
        private const string MasterKey = "settings.masterVolume";
        private const string MusicKey = "settings.musicVolume";
        private const string SfxKey = "settings.sfxVolume";
        private const string QualityKey = "settings.qualityLevel";
        private const string SensitivityKey = "settings.lookSensitivity";

        public static float MasterVolume
        {
            get => PlayerPrefs.GetFloat(MasterKey, 1f);
            set => SetFloat(MasterKey, Mathf.Clamp01(value));
        }

        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(MusicKey, 0.7f);
            set => SetFloat(MusicKey, Mathf.Clamp01(value));
        }

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(SfxKey, 1f);
            set => SetFloat(SfxKey, Mathf.Clamp01(value));
        }

        public static float LookSensitivity
        {
            get => PlayerPrefs.GetFloat(SensitivityKey, 1f);
            set => SetFloat(SensitivityKey, Mathf.Clamp(value, 0.1f, 3f));
        }

        public static int QualityLevel
        {
            get => PlayerPrefs.GetInt(QualityKey, DefaultQualityLevel());
            set
            {
                int clamped = Mathf.Clamp(value, 0, QualitySettings.names.Length - 1);
                PlayerPrefs.SetInt(QualityKey, clamped);
                PlayerPrefs.Save();
                QualitySettings.SetQualityLevel(clamped, true);
                GameEvents.RaiseSettingsChanged();
            }
        }

        /// <summary>Applies stored settings at startup.</summary>
        public static void ApplyAll()
        {
            QualitySettings.SetQualityLevel(QualityLevel, true);
            AudioListener.volume = MasterVolume;

#if UNITY_ANDROID || UNITY_IOS
            // Mobile: cap the frame rate to save battery and reduce heat.
            Application.targetFrameRate = 60;
#elif UNITY_WEBGL
            // WebGL: let the browser drive the frame rate (requestAnimationFrame).
            Application.targetFrameRate = -1;
#endif
        }

        /// <summary>
        /// Picks the starting quality per platform: the "Mobile" quality level for phones and
        /// browsers, "PC" for Windows. Falls back to the first/last level if names change.
        /// </summary>
        private static int DefaultQualityLevel()
        {
            string[] names = QualitySettings.names;
#if UNITY_ANDROID || UNITY_IOS || UNITY_WEBGL
            int index = Array.IndexOf(names, "Mobile");
            return index >= 0 ? index : 0;
#else
            int index = Array.IndexOf(names, "PC");
            return index >= 0 ? index : names.Length - 1;
#endif
        }

        private static void SetFloat(string key, float value)
        {
            PlayerPrefs.SetFloat(key, value);
            PlayerPrefs.Save();
            if (key == MasterKey) AudioListener.volume = value;
            GameEvents.RaiseSettingsChanged();
        }
    }
}
