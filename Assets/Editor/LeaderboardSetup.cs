using System.IO;
using TheLostDungeon.Online;
using UnityEditor;
using UnityEngine;

namespace TheLostDungeon.EditorTools
{
    /// <summary>
    /// Tools > The Lost Dungeon > Create Leaderboard Config
    /// Creates Assets/Resources/LeaderboardConfig.asset and selects it so you can paste the
    /// LootLocker Game API key and leaderboard key.
    /// </summary>
    public static class LeaderboardSetup
    {
        private const string Path = "Assets/Resources/LeaderboardConfig.asset";

        [MenuItem("Tools/The Lost Dungeon/Create Leaderboard Config")]
        public static void Create()
        {
            var config = AssetDatabase.LoadAssetAtPath<LeaderboardConfig>(Path);
            if (config == null)
            {
                Directory.CreateDirectory("Assets/Resources");
                config = ScriptableObject.CreateInstance<LeaderboardConfig>();
                AssetDatabase.CreateAsset(config, Path);
                AssetDatabase.SaveAssets();
            }
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
        }
    }
}
