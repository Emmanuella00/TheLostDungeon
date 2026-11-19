using UnityEngine;

namespace TheLostDungeon.Levels
{
    /// <summary>
    /// Data for one level, as a ScriptableObject asset. Adding a sixth level means creating a
    /// new asset and adding it to the LevelManager's list. No code changes are needed.
    /// </summary>
    [CreateAssetMenu(menuName = "The Lost Dungeon/Level Definition", fileName = "Level_00")]
    public class LevelDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string displayName = "New Level";
        [SerializeField] private string sceneName = "Level_00";
        [SerializeField, TextArea] private string objectiveSummary = "Defeat all enemies.";

        [Header("Level select")]
        [Tooltip("Thumbnail shown on the level select card.")]
        [SerializeField] private Sprite thumbnail;

        [Header("Scoring")]
        [Tooltip("Finish at or under this time for 3 stars; under 1.5x for 2 stars.")]
        [SerializeField, Min(1f)] private float parTimeSeconds = 180f;

        public string DisplayName => displayName;
        public string SceneName => sceneName;
        public string ObjectiveSummary => objectiveSummary;
        public Sprite Thumbnail => thumbnail;
        public float ParTimeSeconds => parTimeSeconds;

#if UNITY_EDITOR
        /// <summary>Editor-only: used by the project setup tool to fill in new assets.</summary>
        public void EditorInitialise(string newDisplayName, string newSceneName, string objective, float parTime)
        {
            displayName = newDisplayName;
            sceneName = newSceneName;
            objectiveSummary = objective;
            parTimeSeconds = parTime;
        }
#endif
    }
}
