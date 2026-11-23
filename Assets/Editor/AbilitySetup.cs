using System;
using TheLostDungeon.Abilities;
using TheLostDungeon.Combat;
using TheLostDungeon.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TheLostDungeon.EditorTools
{
    /// <summary>
    /// Tools > The Lost Dungeon > Add Abilities To Player
    ///     Adds Dash (slot 1, Q, unlocked from the start) and Energy Shield (slot 2, R, locked until
    ///     it's unlocked as a level reward) to the Player prefab, plus a faint shield bubble visual.
    ///     To make the shield a Level 1 reward: on Level_01's LevelObjectives set
    ///     "Unlock Ability On Complete" to: shield
    /// </summary>
    public static class AbilitySetup
    {
        private const string PlayerPath = "Assets/Prefabs/Player.prefab";
        private const string MaterialPath = "Assets/Prefabs/Materials/M_ShieldBubble.mat";

        [MenuItem("Tools/The Lost Dungeon/Add Abilities To Player")]
        public static void AddAbilities()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath) == null)
            {
                EditorUtility.DisplayDialog("Abilities", "Player prefab not found at " + PlayerPath, "OK");
                return;
            }

            Material bubbleMat = BubbleMaterial();
            GameObject contents = PrefabUtility.LoadPrefabContents(PlayerPath);
            try
            {
                Transform view = contents.transform.Find("CameraPivot");

                DashAbility dash = contents.GetComponent<DashAbility>();
                if (dash == null) dash = contents.AddComponent<DashAbility>();
                dash.EditorConfigure("dash", "Dash", 3f);

                ShieldAbility shield = contents.GetComponent<ShieldAbility>();
                if (shield == null) shield = contents.AddComponent<ShieldAbility>();
                shield.EditorConfigure("shield", "Energy Shield", 10f);
                SetProp(shield, "visual", p => p.objectReferenceValue = Bubble(view, bubbleMat));

                PlayerAbilities abilities = contents.GetComponent<PlayerAbilities>();
                if (abilities == null) abilities = contents.AddComponent<PlayerAbilities>();
                SetProp(abilities, "controller", p => p.objectReferenceValue = contents.GetComponent<PlayerController>());
                SetProp(abilities, "view", p => p.objectReferenceValue = view);
                SetProp(abilities, "slots", p =>
                {
                    p.arraySize = 2;
                    p.GetArrayElementAtIndex(0).FindPropertyRelative("ability").objectReferenceValue = dash;
                    p.GetArrayElementAtIndex(0).FindPropertyRelative("unlockedByDefault").boolValue = true;
                    p.GetArrayElementAtIndex(1).FindPropertyRelative("ability").objectReferenceValue = shield;
                    p.GetArrayElementAtIndex(1).FindPropertyRelative("unlockedByDefault").boolValue = false;
                });

                PrefabUtility.SaveAsPrefabAsset(contents, PlayerPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Abilities",
                "Player now has:\n• Q: Dash (unlocked)\n• R: Energy Shield (locked)\n\n" +
                "Make the shield a Level 1 reward: select LevelObjectives in Level_01 and set " +
                "'Unlock Ability On Complete' to: shield\n\n" +
                "For quick testing, the debug panel (F1) has an 'Unlock all abilities' button.",
                "OK");
        }

        private static GameObject Bubble(Transform parent, Material material)
        {
            Transform existing = parent != null ? parent.Find("ShieldBubble") : null;
            if (existing != null) return existing.gameObject;

            GameObject bubble = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bubble.name = "ShieldBubble";
            Object.DestroyImmediate(bubble.GetComponent<Collider>());
            bubble.transform.SetParent(parent, false);
            bubble.transform.localScale = Vector3.one * 1.6f;
            var r = bubble.GetComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            bubble.SetActive(false);
            return bubble;
        }

        /// <summary>Faint additive cyan, drawn on both sides so it's visible from inside the bubble.</summary>
        private static Material BubbleMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (existing != null) return existing;

            var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            m.SetColor("_BaseColor", new Color(0.15f, 0.45f, 0.7f, 0.35f));
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 2f);   // additive
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.One);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_Cull", 0f);    // both sides
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            AssetDatabase.CreateAsset(m, MaterialPath);
            return m;
        }

        private static void SetProp(Object target, string property, Action<SerializedProperty> set)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(property);
            if (prop == null)
            {
                Debug.LogWarning($"[AbilitySetup] {target.GetType().Name} has no field '{property}'.");
                return;
            }
            set(prop);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
