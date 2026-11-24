using System.Collections.Generic;
using System.IO;
using TheLostDungeon.UI;
using UnityEditor;
using UnityEngine;

namespace TheLostDungeon.EditorTools
{
    /// <summary>
    /// Tools > The Lost Dungeon > Bake UI Prefab (editable)
    ///
    ///   1. Saves every generated icon as a PNG sprite in Assets/UI/Icons and lists them in
    ///      Assets/Resources/UIIconSet.asset, so icons can be repainted or swapped.
    ///   2. Builds the complete UI (all screens) in the editor and saves it as
    ///      Assets/Resources/GameUI.prefab.
    ///
    /// From then on the game loads that prefab. Open it, change anything visual (move, resize,
    /// recolour, change fonts and texts, swap sprites) and the game keeps your changes: the screen
    /// code only finds elements by NAME and attaches behaviour. Don't rename or delete elements.
    /// </summary>
    public static class UIPrefabBaker
    {
        private const string IconFolder = "Assets/UI/Icons";
        private const string ResourcesFolder = "Assets/Resources";
        private const string IconSetPath = ResourcesFolder + "/UIIconSet.asset";
        private static readonly string PrefabPath = $"{ResourcesFolder}/{UIManager.PrefabName}.prefab";

        [MenuItem("Tools/The Lost Dungeon/Bake UI Prefab (editable)")]
        public static void Bake()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Bake UI Prefab", "Stop Play mode first.", "OK");
                return;
            }

            if (File.Exists(PrefabPath) &&
                !EditorUtility.DisplayDialog("Bake UI Prefab",
                    "GameUI.prefab already exists.\n\nBaking again REPLACES it and loses the edits you made in it. " +
                    "(Use 'Add Missing UI Elements' instead to keep your edits.)",
                    "Replace", "Cancel"))
            {
                return;
            }

            ExportIcons();
            BuildPrefab(replace: true);

            EditorUtility.DisplayDialog("Bake UI Prefab",
                "Done.\n\n" +
                $"• UI prefab: {PrefabPath}\n" +
                $"• Icons: {IconFolder} (listed in {IconSetPath})\n\n" +
                "Open the prefab to edit the layout. Screens are saved switched off: tick a screen " +
                "(e.g. HudScreen) to see it while editing. Its on/off state doesn't matter in game.\n\n" +
                "Rule: you can change anything VISUAL, but don't rename or delete elements, the code finds them by name.",
                "OK");
        }

        /// <summary>
        /// Adds elements that newer code expects but the prefab doesn't have yet, WITHOUT touching
        /// anything you already edited (existing elements are reused as they are).
        /// </summary>
        [MenuItem("Tools/The Lost Dungeon/Add Missing UI Elements To Prefab")]
        public static void AddMissing()
        {
            if (!File.Exists(PrefabPath))
            {
                Bake();
                return;
            }
            BuildPrefab(replace: false);
            EditorUtility.DisplayDialog("UI prefab", "Missing elements added. Your edits were kept.", "OK");
        }

        private static void BuildPrefab(bool replace)
        {
            IconFactory.ResetCache();
            IconFactory.IgnoreIconSet = false;   // reference the saved icon assets, not temporary ones

            bool editExisting = !replace && File.Exists(PrefabPath);
            GameObject root = editExisting
                ? PrefabUtility.LoadPrefabContents(PrefabPath)
                : new GameObject(UIManager.PrefabName);

            try
            {
                UIManager.ConfigureCanvas(root);
                UIManager.CreateScreens(null, root.transform);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                if (editExisting) PrefabUtility.UnloadPrefabContents(root);
                else Object.DestroyImmediate(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void ExportIcons()
        {
            Directory.CreateDirectory(IconFolder);
            Directory.CreateDirectory(ResourcesFolder);

            // 1. Write the generated icons as PNG files.
            IconFactory.ResetCache();
            IconFactory.IgnoreIconSet = true;
            var paths = new Dictionary<string, string>();
            try
            {
                foreach (KeyValuePair<string, System.Func<Sprite>> icon in IconFactory.All())
                {
                    Sprite sprite = icon.Value();
                    string path = $"{IconFolder}/Icon_{icon.Key}.png";
                    File.WriteAllBytes(path, sprite.texture.EncodeToPNG());
                    paths[icon.Key] = path;
                }
            }
            finally
            {
                IconFactory.IgnoreIconSet = false;
                IconFactory.ResetCache();
            }
            AssetDatabase.Refresh();

            // 2. Import them as UI sprites (the panel gets 9-slice borders).
            foreach (KeyValuePair<string, string> entry in paths)
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(entry.Value);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                if (entry.Key == "panel")
                {
                    int b = IconFactory.PanelBorder;
                    importer.spriteBorder = new Vector4(b, b, b, b);
                }
                importer.SaveAndReimport();
            }

            // 3. List them in the icon set asset the game reads.
            UIIconSet set = AssetDatabase.LoadAssetAtPath<UIIconSet>(IconSetPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<UIIconSet>();
                AssetDatabase.CreateAsset(set, IconSetPath);
            }
            foreach (KeyValuePair<string, string> entry in paths)
            {
                set.EditorSet(entry.Key, AssetDatabase.LoadAssetAtPath<Sprite>(entry.Value));
            }
            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
        }
    }
}
