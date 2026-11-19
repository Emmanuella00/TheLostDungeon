using System;
using System.Collections.Generic;
using System.IO;
using TheLostDungeon.Data;
using TheLostDungeon.Events;
using UnityEngine;

namespace TheLostDungeon.Core
{
    /// <summary>
    /// Loads and saves the JSON progress file. Other systems either read SaveManager.Instance.Data
    /// directly (level progression, stats) or implement ISaveable and register themselves.
    /// </summary>
    public class SaveManager : Singleton<SaveManager>
    {
        [SerializeField] private string fileName = "save.json";
        [SerializeField] private string webGlPrefsKey = "TheLostDungeon.Save";

        private readonly List<ISaveable> saveables = new List<ISaveable>();
        private ISaveStorage storage;

        public SaveData Data { get; private set; } = new SaveData();
        public bool HasSaveFile => storage != null && storage.Exists();

        protected override void OnSingletonAwake()
        {
            storage = CreateStorageForPlatform();
            Load();
        }

        /// <summary>
        /// CONDITIONAL COMPILATION: WebGL has no normal file system, so the same JSON is kept in
        /// PlayerPrefs (browser storage). Every other platform writes a real file. The rest
        /// of the game only sees ISaveStorage and never knows the difference.
        /// </summary>
        private ISaveStorage CreateStorageForPlatform()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return new PlayerPrefsSaveStorage(webGlPrefsKey);
#else
            return new FileSaveStorage(Path.Combine(Application.persistentDataPath, fileName));
#endif
        }

        public void Register(ISaveable saveable)
        {
            if (saveable == null || saveables.Contains(saveable)) return;
            saveables.Add(saveable);
            saveable.RestoreState(Data);
        }

        public void Unregister(ISaveable saveable) => saveables.Remove(saveable);

        public void Save()
        {
            try
            {
                foreach (ISaveable saveable in saveables) saveable.CaptureState(Data);

                Data.version = SaveData.CurrentVersion;
                Data.lastSavedUtc = DateTime.UtcNow.ToString("o");
                storage.Write(SaveSerializer.ToJson(Data));
                GameEvents.RaiseProgressSaved();
            }
            catch (Exception e)
            {
                // A failed save must never crash the game.
                Debug.LogError($"[SaveManager] Save failed: {e.Message}");
            }
        }

        public void Load()
        {
            SaveData loaded = null;
            try
            {
                if (storage.Exists()) loaded = SaveSerializer.FromJson(storage.Read());
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveManager] Load failed, starting a new save. {e.Message}");
            }

            Data = loaded ?? new SaveData();
            foreach (ISaveable saveable in saveables) saveable.RestoreState(Data);
        }

        /// <summary>Wipes all progress (used by "Start Game" / new game).</summary>
        public void ResetProgress()
        {
            Data = new SaveData();
            storage.Delete();
            foreach (ISaveable saveable in saveables) saveable.RestoreState(Data);
        }

        // Mobile apps are often killed without OnApplicationQuit, so save when sent to background.
        private void OnApplicationPause(bool paused)
        {
            if (paused) Save();
        }

        private void OnApplicationQuit() => Save();
    }
}
