using UnityEngine;

namespace TheLostDungeon.Online
{
    /// <summary>
    /// Settings for the online leaderboard (Assets/Resources/LeaderboardConfig.asset).
    /// Fill in the Game API key and leaderboard key from the LootLocker web console.
    /// With an empty key the game still works, using a local-only leaderboard.
    /// </summary>
    [CreateAssetMenu(fileName = "LeaderboardConfig", menuName = "The Lost Dungeon/Leaderboard Config")]
    public class LeaderboardConfig : ScriptableObject
    {
        [Tooltip("LootLocker > Settings > API Keys > Game API key (starts with dev_ or prod_).")]
        public string gameKey = "";

        [Tooltip("The leaderboard's KEY (not its name) from LootLocker > Leaderboards.")]
        public string leaderboardKey = "lost_dungeon_campaign";

        [Tooltip("Shown in LootLocker's player list; bump it when you release a new build.")]
        public string gameVersion = "1.0.0";

        public string baseUrl = "https://api.lootlocker.io";

        [Min(2)] public int timeoutSeconds = 10;

        [Min(1)] public int entriesToShow = 10;

        public bool IsConfigured => !string.IsNullOrWhiteSpace(gameKey) && !string.IsNullOrWhiteSpace(leaderboardKey);
    }
}
