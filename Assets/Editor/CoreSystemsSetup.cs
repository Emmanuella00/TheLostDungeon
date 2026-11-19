using System.IO;
using TheLostDungeon.Audio;
using TheLostDungeon.Core;
using TheLostDungeon.Levels;
using UnityEditor;
using UnityEngine;

namespace TheLostDungeon.EditorTools
{
    /// <summary>
    /// Tools > The Lost Dungeon > Setup Core Systems
    /// Creates (or refreshes) the five Level Definition assets, the Sound Library asset and the
    /// Systems prefab that SystemsBootstrapper spawns at startup. Safe to run more than once:
    /// existing Level Definition and Sound Library assets are kept (your edits survive), while
    /// the Systems prefab is rebuilt so it always has every manager wired up.
    /// </summary>
    public static class CoreSystemsSetup
    {
        private const string Root = "Assets";
        private const string LevelsFolder = Root + "/Data/Levels";
        private const string AudioFolder = Root + "/Data/Audio";
        private const string ResourcesFolder = Root + "/Resources";
        private const string SystemsPrefabPath = ResourcesFolder + "/Systems.prefab";
        private const string SoundLibraryPath = AudioFolder + "/SoundLibrary.asset";

        private static readonly (string name, string scene, string objective, float par)[] DefaultLevels =
        {
            ("Loading Bay",       "Level_01", "Defeat all warrior bots in the loading bay.",          150f),
            ("Storage Racks",     "Level_02", "Find the keycard and open the bay door.",               200f),
            ("Catwalks",          "Level_03", "Activate the three power switches.",                    240f),
            ("Ducts & Corridors", "Level_04", "Survive the lockdown until the doors reopen.",           240f),
            ("Reactor Core",      "Level_05", "Destroy the reactor guardian.",                         300f),
        };

        [MenuItem("Tools/The Lost Dungeon/Setup Core Systems")]
        public static void Setup()
        {
            EnsureFolder(LevelsFolder);
            EnsureFolder(AudioFolder);
            EnsureFolder(ResourcesFolder);

            LevelDefinition[] levels = CreateLevelAssets();
            SoundLibrary library = LoadOrCreate<SoundLibrary>(SoundLibraryPath);
            CreateSystemsPrefab(levels, library);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("The Lost Dungeon",
                "Core systems are set up:\n\n" +
                "• 5 Level Definitions in " + LevelsFolder + "\n" +
                "• Sound Library in " + AudioFolder + "\n" +
                "• Systems prefab in " + ResourcesFolder + "\n\n" +
                "Next: create scenes named MainMenu and Level_01..Level_05 and add them to the Build Profiles scene list.",
                "OK");
        }

        private static LevelDefinition[] CreateLevelAssets()
        {
            var result = new LevelDefinition[DefaultLevels.Length];

            for (int i = 0; i < DefaultLevels.Length; i++)
            {
                var (name, scene, objective, par) = DefaultLevels[i];
                string path = $"{LevelsFolder}/{scene}.asset";

                LevelDefinition level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
                if (level == null)
                {
                    level = ScriptableObject.CreateInstance<LevelDefinition>();
                    level.EditorInitialise(name, scene, objective, par);
                    AssetDatabase.CreateAsset(level, path);
                }

                result[i] = level;
            }

            return result;
        }

        private static void CreateSystemsPrefab(LevelDefinition[] levels, SoundLibrary library)
        {
            var root = new GameObject("Systems");
            try
            {
                root.AddComponent<GameManager>();
                root.AddComponent<SaveManager>();
                LevelManager levelManager = root.AddComponent<LevelManager>();
                AudioManager audioManager = root.AddComponent<AudioManager>();
                root.AddComponent<PlayerStatsTracker>();

                // Fill private [SerializeField] fields the same way the Inspector does.
                var levelSo = new SerializedObject(levelManager);
                SerializedProperty levelsProp = levelSo.FindProperty("levels");
                levelsProp.arraySize = levels.Length;
                for (int i = 0; i < levels.Length; i++)
                {
                    levelsProp.GetArrayElementAtIndex(i).objectReferenceValue = levels[i];
                }
                levelSo.ApplyModifiedPropertiesWithoutUndo();

                var audioSo = new SerializedObject(audioManager);
                audioSo.FindProperty("library").objectReferenceValue = library;
                audioSo.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, SystemsPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
