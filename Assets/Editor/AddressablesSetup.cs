using System.Collections.Generic;
using System.IO;
using System.Linq;
using TheLostDungeon.Enemies;
using TheLostDungeon.Levels;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace TheLostDungeon.EditorTools
{
    /// <summary>
    /// Tools > The Lost Dungeon > Setup Addressables
    ///     - creates the Addressables settings and three groups: Levels, Enemies, Music
    ///     - marks the five level scenes Addressable (address = scene name, label "level") and links
    ///       each LevelDefinition to its scene
    ///     - marks the enemy prefabs Addressable (label "enemy") and points the EnemySpawner prefab at them
    ///     - removes the level scenes from the Build Profiles scene list (they now come from
    ///       Addressables; MainMenu stays as the built-in start scene)
    ///     - turns on "Build Addressables on Player Build" so every player build includes fresh content
    ///
    /// Tools > The Lost Dungeon > Build Addressables Content
    ///     Builds the content for the CURRENT platform (switch platform first for WebGL/Android).
    /// </summary>
    public static class AddressablesSetup
    {
        private const string LevelsFolder = "Assets/Data/Levels";
        private const string ScenesFolder = "Assets/Scenes";
        private const string EnemyFolder = "Assets/Prefabs/Enemies";
        private const string SpawnerPath = EnemyFolder + "/EnemySpawner.prefab";

        [MenuItem("Tools/The Lost Dungeon/Setup Addressables")]
        public static void Setup()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.BuildWithPlayer;
            settings.AddLabel("level");
            settings.AddLabel("enemy");

            AddressableAssetGroup levelGroup = GetOrCreateGroup(settings, "Levels");
            AddressableAssetGroup enemyGroup = GetOrCreateGroup(settings, "Enemies");
            GetOrCreateGroup(settings, "Music");   // level music lands here when you assign it

            var report = new List<string>();
            var addressableScenePaths = new HashSet<string>();

            // 1. Level scenes
            foreach (LevelDefinition level in LoadAll<LevelDefinition>(LevelsFolder))
            {
                string scenePath = $"{ScenesFolder}/{level.SceneName}.unity";
                string guid = AssetDatabase.AssetPathToGUID(scenePath);
                if (string.IsNullOrEmpty(guid))
                {
                    report.Add($"• {level.DisplayName}: scene {scenePath} not found (skipped)");
                    continue;
                }

                AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, levelGroup, false, false);
                entry.address = level.SceneName;
                entry.SetLabel("level", true, false, false);
                level.EditorSetSceneAsset(guid);
                EditorUtility.SetDirty(level);
                addressableScenePaths.Add(scenePath);
                report.Add($"• {level.DisplayName} → {level.SceneName}");
            }

            // 2. Enemy prefabs
            var enemyGuids = new Dictionary<string, string>();
            foreach (string name in new[] { "Enemy_Warrior", "Enemy_Archer", "Enemy_Guardian" })
            {
                string guid = AssetDatabase.AssetPathToGUID($"{EnemyFolder}/{name}.prefab");
                if (string.IsNullOrEmpty(guid)) continue;
                AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, enemyGroup, false, false);
                entry.address = name;
                entry.SetLabel("enemy", true, false, false);
                enemyGuids[name] = guid;
            }

            // 3. Spawner prefab uses Addressable references (Warrior, Warrior, Archer)
            if (enemyGuids.ContainsKey("Enemy_Warrior") && enemyGuids.ContainsKey("Enemy_Archer"))
            {
                LinkSpawner(new[] { enemyGuids["Enemy_Warrior"], enemyGuids["Enemy_Warrior"], enemyGuids["Enemy_Archer"] });
            }

            // 4. Level scenes now come from Addressables, not the built-in scene list
            EditorBuildSettings.scenes = EditorBuildSettings.scenes
                .Where(s => !addressableScenePaths.Contains(s.path))
                .ToArray();
            EnsureMainMenuFirst();

            EditorUtility.SetDirty(levelGroup);
            EditorUtility.SetDirty(enemyGroup);
            EditorUtility.SetDirty(settings);
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("Addressables",
                "Addressables are set up.\n\nAddressable levels:\n" + string.Join("\n", report) +
                $"\n\nAddressable enemies: {enemyGuids.Count}\n" +
                "Level scenes were removed from the Build Profiles scene list (MainMenu stays).\n" +
                "'Build Addressables on Player Build' is ON.\n\n" +
                "Optional: give a level its own music in its Level Definition (Music field).\n" +
                "Then: Tools > The Lost Dungeon > Build Addressables Content.",
                "OK");
        }

        [MenuItem("Tools/The Lost Dungeon/Build Addressables Content")]
        public static void BuildContent()
        {
            AddressableAssetSettings.CleanPlayerContent(AddressableAssetSettingsDefaultObject.Settings.ActivePlayerDataBuilder);
            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);

            bool ok = string.IsNullOrEmpty(result.Error);
            EditorUtility.DisplayDialog("Addressables build",
                ok
                    ? $"Content built for {EditorUserBuildSettings.activeBuildTarget} in {result.Duration:0.0}s.\n\n" +
                      "Switch platform (Windows / WebGL / Android) and build again for each target, " +
                      "or just build the player: content is built automatically with it."
                    : "Build failed:\n" + result.Error,
                "OK");
        }

        private static void LinkSpawner(string[] guids)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(SpawnerPath) == null) return;

            GameObject contents = PrefabUtility.LoadPrefabContents(SpawnerPath);
            try
            {
                var spawner = contents.GetComponent<EnemySpawner>();
                var so = new SerializedObject(spawner);
                SerializedProperty list = so.FindProperty("enemies");
                list.arraySize = guids.Length;
                for (int i = 0; i < guids.Length; i++)
                {
                    list.GetArrayElementAtIndex(i).FindPropertyRelative("m_AssetGUID").stringValue = guids[i];
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contents, SpawnerPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void EnsureMainMenuFirst()
        {
            string menuPath = $"{ScenesFolder}/MainMenu.unity";
            if (!File.Exists(menuPath)) return;

            var scenes = EditorBuildSettings.scenes.Where(s => s.path != menuPath).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(menuPath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static AddressableAssetGroup GetOrCreateGroup(AddressableAssetSettings settings, string name)
        {
            AddressableAssetGroup group = settings.FindGroup(name);
            if (group == null) group = settings.CreateGroup(name, false, false, true, settings.DefaultGroup.Schemas);

            // AssetReferences load by GUID, so the GUID must be in the catalog. Also make sure the
            // group is actually built and included.
            BundledAssetGroupSchema bundled = group.GetSchema<BundledAssetGroupSchema>();
            if (bundled == null) bundled = group.AddSchema<BundledAssetGroupSchema>();
            bundled.IncludeInBuild = true;
            bundled.IncludeGUIDInCatalog = true;
            bundled.IncludeAddressInCatalog = true;
            EditorUtility.SetDirty(bundled);
            EditorUtility.SetDirty(group);
            return group;
        }

        private static IEnumerable<T> LoadAll<T>(string folder) where T : Object =>
            AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<T>)
                .Where(a => a != null);
    }
}