using UnityEngine;

namespace TheLostDungeon.Items
{
    /// <summary>
    /// Data for one kind of item (Red Keycard, Credits...). A new item is a new asset,
    /// not new code: Create > The Lost Dungeon > Item Definition.
    /// </summary>
    [CreateAssetMenu(menuName = "The Lost Dungeon/Item Definition", fileName = "Item_")]
    public class ItemDefinition : ScriptableObject
    {
        [Tooltip("Unique id used in saves and objectives, e.g. keycard_red. Never change it after release.")]
        [SerializeField] private string id = "item";
        [SerializeField] private string displayName = "Item";
        [SerializeField] private Sprite icon;
        [SerializeField, Min(1)] private int maxStack = 99;

        [Tooltip("Kept between levels and saved (credits). Off = only lasts for the current level (keycards).")]
        [SerializeField] private bool persistent;

        public string Id => id;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public int MaxStack => maxStack;
        public bool Persistent => persistent;

#if UNITY_EDITOR
        public void EditorInitialise(string newId, string newName, int newMaxStack, bool isPersistent)
        {
            id = newId;
            displayName = newName;
            maxStack = newMaxStack;
            persistent = isPersistent;
        }
#endif
    }
}
