using UnityEngine;

namespace TheLostDungeon.Core
{
    /// <summary>
    /// Creates the "Systems" prefab (GameManager, LevelManager, SaveManager, AudioManager,
    /// PlayerStatsTracker) automatically before the first scene loads, so you can press Play
    /// in ANY scene (menu or a level) and all global services already exist.
    ///
    /// The prefab is made for you by Tools > The Lost Dungeon > Setup Core Systems and lives at
    /// Assets/Resources/Systems.prefab.
    /// </summary>
    public static class SystemsBootstrapper
    {
        private const string SystemsPrefabPath = "Systems";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateSystems()
        {
            if (GameManager.HasInstance) return;

            GameObject prefab = Resources.Load<GameObject>(SystemsPrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning("[SystemsBootstrapper] No Systems prefab found. " +
                                 "Run Tools > The Lost Dungeon > Setup Core Systems.");
                return;
            }

            GameObject systems = Object.Instantiate(prefab);
            systems.name = "Systems";
        }
    }
}
