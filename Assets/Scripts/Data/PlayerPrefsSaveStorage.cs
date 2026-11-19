using UnityEngine;

namespace TheLostDungeon.Data
{
    /// <summary>
    /// Stores the same JSON text inside PlayerPrefs. Used for WebGL builds, where Unity keeps
    /// PlayerPrefs in the browser's storage and it reliably survives closing the tab.
    /// </summary>
    public class PlayerPrefsSaveStorage : ISaveStorage
    {
        private readonly string key;

        public PlayerPrefsSaveStorage(string key)
        {
            this.key = key;
        }

        public bool Exists() => PlayerPrefs.HasKey(key);

        public string Read() => PlayerPrefs.GetString(key, null);

        public void Write(string json)
        {
            PlayerPrefs.SetString(key, json);
            PlayerPrefs.Save();
        }

        public void Delete()
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }
}
