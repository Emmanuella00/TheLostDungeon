using System;
using System.Collections.Generic;
using UnityEngine;

namespace TheLostDungeon.UI
{
    /// <summary>
    /// The UI's icon sprites as editable assets (Resources/UIIconSet). Created by
    /// Tools > The Lost Dungeon > Bake UI Prefab. Replace any sprite here, or repaint its PNG in
    /// Assets/UI/Icons, and every screen that uses that icon updates.
    /// </summary>
    [CreateAssetMenu(fileName = "UIIconSet", menuName = "The Lost Dungeon/UI Icon Set")]
    public class UIIconSet : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string key;
            public Sprite sprite;
        }

        [SerializeField] private List<Entry> icons = new List<Entry>();

        public Sprite Find(string key)
        {
            foreach (Entry entry in icons)
            {
                if (entry.key == key) return entry.sprite;
            }
            return null;
        }

#if UNITY_EDITOR
        public void EditorSet(string key, Sprite sprite)
        {
            Entry existing = icons.Find(e => e.key == key);
            if (existing != null) existing.sprite = sprite;
            else icons.Add(new Entry { key = key, sprite = sprite });
        }
#endif
    }
}
