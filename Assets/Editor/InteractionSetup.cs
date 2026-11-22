using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TheLostDungeon.Combat;
using TheLostDungeon.Hazards;
using TheLostDungeon.Interaction;
using TheLostDungeon.Items;
using TheLostDungeon.Objectives;
using TheLostDungeon.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TheLostDungeon.EditorTools
{
    /// <summary>
    /// Tools > The Lost Dungeon > Create Interaction Prefabs
    ///     Builds ready-to-place prefabs in Assets/Prefabs/Interactables using the Quaternius models:
    ///     keycard / health / credit pickups, a loot chest, an explosive barrel, three kinds of door
    ///     and a wall switch, plus the item and loot-table assets they use. Also gives the Player
    ///     prefab its PlayerInventory and PlayerInteractor.
    ///
    /// Tools > The Lost Dungeon > Add Level Objectives To Open Scene
    ///     Adds a LevelObjectives object with two example objectives (destroy 3 bots, reach the exit).
    /// </summary>
    public static class InteractionSetup
    {
        private const string ModelFolder = "Assets/Quaternius";
        private const string PrefabFolder = "Assets/Prefabs/Interactables";
        private const string ItemFolder = "Assets/Data/Items";
        private const string LootFolder = "Assets/Data/Loot";
        private const string MaterialFolder = "Assets/Prefabs/Materials";
        private const string EffectsFolder = "Assets/Prefabs/Effects";
        private const string PlayerPath = "Assets/Prefabs/Player.prefab";

        private static readonly Color Red = new Color(1f, 0.2f, 0.15f);
        private static readonly Color Blue = new Color(0.2f, 0.55f, 1f);
        private static readonly Color Orange = new Color(1f, 0.55f, 0.1f);

        [MenuItem("Tools/The Lost Dungeon/Create Interaction Prefabs")]
        public static void CreateInteractionPrefabs()
        {
            EnsureFolder(PrefabFolder);
            EnsureFolder(ItemFolder);
            EnsureFolder(LootFolder);
            EnsureFolder(MaterialFolder);

            // Items
            ItemDefinition red = Item("Item_KeycardRed", "keycard_red", "Red Keycard", 1, false);
            ItemDefinition blue = Item("Item_KeycardBlue", "keycard_blue", "Blue Keycard", 1, false);
            ItemDefinition credits = Item("Item_Credits", "credits", "Credits", 99999, true);

            // Materials
            Material doorMat = Lit("M_Door", new Color(0.22f, 0.24f, 0.27f), 0.6f, 0.5f, null);
            Material frameMat = Lit("M_DoorFrame", new Color(0.12f, 0.13f, 0.15f), 0.7f, 0.4f, null);
            Material stripeOrange = Lit("M_Stripe_Orange", Orange, 0f, 0.5f, Orange * 2f);
            Material stripeRed = Lit("M_Stripe_Red", Red, 0f, 0.5f, Red * 2f);
            Material stripeBlue = Lit("M_Stripe_Blue", Blue, 0f, 0.5f, Blue * 2f);
            Material indicatorMat = Lit("M_SwitchIndicator", Red, 0f, 0.5f, Red * 2f);

            // Pickups
            GameObject redCard = PickupPrefab("Pickup_KeycardRed", "Prop_KeyCard", Pickup.PickupKind.Item, red, 1, Red);
            GameObject blueCard = PickupPrefab("Pickup_KeycardBlue", "Prop_KeyCard", Pickup.PickupKind.Item, blue, 1, Blue);
            GameObject health = PickupPrefab("Pickup_HealthPack", "Prop_HealthPack", Pickup.PickupKind.Health, null, 1, new Color(0.3f, 1f, 0.4f));
            GameObject creditPickup = PickupPrefab("Pickup_Credits", "Prop_Ammo_Small", Pickup.PickupKind.Item, credits, 25, new Color(1f, 0.85f, 0.2f));

            // Loot table: mostly credits, sometimes health
            LootTable loot = LootTableAsset("LT_Chest", new List<LootTable.Entry>
            {
                new LootTable.Entry { pickupPrefab = creditPickup, weight = 60f },
                new LootTable.Entry { pickupPrefab = health, weight = 30f },
            });

            ChestPrefab(loot);
            BarrelPrefab(ExplosionEffect());
            DoorPrefab("Door_Sliding", doorMat, frameMat, stripeOrange, null, playerCanOpen: true);
            DoorPrefab("Door_Locked_Red", doorMat, frameMat, stripeRed, red, playerCanOpen: true);
            DoorPrefab("Door_Locked_Blue", doorMat, frameMat, stripeBlue, blue, playerCanOpen: true);
            DoorPrefab("Door_SwitchControlled", doorMat, frameMat, stripeOrange, null, playerCanOpen: false);
            SwitchPrefab(frameMat, indicatorMat);

            bool playerUpdated = AddPlayerComponents(credits);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Interaction prefabs",
                "Created in " + PrefabFolder + ":\n" +
                "• Pickup_KeycardRed / Blue, Pickup_HealthPack, Pickup_Credits\n" +
                "• Chest (drops loot from LT_Chest)\n" +
                "• ExplosiveBarrel (+ FX_Explosion)\n" +
                "• Door_Sliding, Door_Locked_Red / Blue, Door_SwitchControlled\n" +
                "• WallSwitch\n\n" +
                "Items in " + ItemFolder + ", loot table in " + LootFolder + ".\n" +
                (playerUpdated ? "Player prefab now has PlayerInventory and PlayerInteractor." : "Player prefab not found."),
                "OK");
        }

        // ------------------------------------------------------------------ prefabs

        private static GameObject PickupPrefab(string name, string model, Pickup.PickupKind kind, ItemDefinition item, int amount, Color glow)
        {
            var root = new GameObject(name);
            try
            {
                var trigger = root.AddComponent<SphereCollider>();
                trigger.isTrigger = true;
                trigger.radius = 0.6f;
                trigger.center = new Vector3(0f, 0.5f, 0f);

                Transform visual = Child(root.transform, "Visual", new Vector3(0f, 0.5f, 0f));
                InstantiateModel(model, visual);
                GlowLight(visual, glow);

                Pickup pickup = root.AddComponent<Pickup>();
                SetProp(pickup, "kind", p => p.enumValueIndex = (int)kind);
                SetProp(pickup, "item", p => p.objectReferenceValue = item);
                SetProp(pickup, "amount", p => p.intValue = amount);
                SetProp(pickup, "visual", p => p.objectReferenceValue = visual);

                return Save(root, name);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void ChestPrefab(LootTable loot)
        {
            var root = new GameObject("Chest");
            try
            {
                GameObject model = InstantiateModel("Prop_Chest", root.transform);
                FitBoxCollider(root, isTrigger: false);

                Transform lid = FindChild(model.transform, "Chest_Top") ?? FindChildContaining(model.transform, "Lid");
                Transform spawn = Child(root.transform, "LootSpawn", new Vector3(0f, 0.3f, 0.9f));

                Chest chest = root.AddComponent<Chest>();
                SetProp(chest, "lootTable", p => p.objectReferenceValue = loot);
                SetProp(chest, "spawnPoint", p => p.objectReferenceValue = spawn);
                SetProp(chest, "lid", p => p.objectReferenceValue = lid);

                Save(root, "Chest");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void BarrelPrefab(GameObject explosion)
        {
            var root = new GameObject("ExplosiveBarrel");
            try
            {
                InstantiateModel("Prop_Barrel2_Closed", root.transform);
                FitBoxCollider(root, isTrigger: false);

                Health health = root.AddComponent<Health>();
                SetProp(health, "maxHealth", p => p.floatValue = 30f);

                ExplosiveBarrel barrel = root.AddComponent<ExplosiveBarrel>();
                SetProp(barrel, "explosionEffect", p => p.objectReferenceValue = explosion);
                root.AddComponent<DamageFlash>();

                Save(root, "ExplosiveBarrel");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>A 2.45 m x 3 m doorway whose panel sinks into the floor when opened.</summary>
        private static void DoorPrefab(string name, Material panelMat, Material frameMat, Material stripeMat, ItemDefinition key, bool playerCanOpen)
        {
            var root = new GameObject(name);
            try
            {
                Cube(root.transform, "Frame_Left", new Vector3(-1.35f, 1.5f, 0f), new Vector3(0.25f, 3f, 0.4f), frameMat, keepCollider: true);
                Cube(root.transform, "Frame_Right", new Vector3(1.35f, 1.5f, 0f), new Vector3(0.25f, 3f, 0.4f), frameMat, keepCollider: true);
                Cube(root.transform, "Frame_Top", new Vector3(0f, 3.15f, 0f), new Vector3(2.95f, 0.3f, 0.4f), frameMat, keepCollider: true);

                Transform panel = Child(root.transform, "Panel", Vector3.zero);
                Cube(panel, "PanelBody", new Vector3(0f, 1.5f, 0f), new Vector3(2.45f, 3f, 0.15f), panelMat, keepCollider: true);
                Cube(panel, "Stripe_Front", new Vector3(0f, 1.6f, 0.08f), new Vector3(1.6f, 0.08f, 0.02f), stripeMat, keepCollider: false);
                Cube(panel, "Stripe_Back", new Vector3(0f, 1.6f, -0.08f), new Vector3(1.6f, 0.08f, 0.02f), stripeMat, keepCollider: false);

                Door door = root.AddComponent<Door>();
                SetProp(door, "panel", p => p.objectReferenceValue = panel);
                SetProp(door, "openOffset", p => p.vector3Value = new Vector3(0f, -3.05f, 0f));
                SetProp(door, "playerCanOpen", p => p.boolValue = playerCanOpen);
                SetProp(door, "requiredKey", p => p.objectReferenceValue = key);

                Save(root, name);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>Wall panel with a button. The button faces +Z (blue arrow): point that away from the wall.</summary>
        private static void SwitchPrefab(Material panelMat, Material indicatorMat)
        {
            var root = new GameObject("WallSwitch");
            try
            {
                var box = root.AddComponent<BoxCollider>();
                box.center = new Vector3(0f, 1.3f, 0f);
                box.size = new Vector3(0.5f, 0.7f, 0.2f);

                Cube(root.transform, "Panel", new Vector3(0f, 1.3f, 0f), new Vector3(0.5f, 0.7f, 0.1f), panelMat, keepCollider: false);
                GameObject button = Cube(root.transform, "Button", new Vector3(0f, 1.38f, 0.07f), new Vector3(0.18f, 0.18f, 0.06f), indicatorMat, keepCollider: false);

                WallSwitch wallSwitch = root.AddComponent<WallSwitch>();
                SetProp(wallSwitch, "indicator", p => p.objectReferenceValue = button.GetComponent<Renderer>());

                Save(root, "WallSwitch");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject ExplosionEffect()
        {
            var additive = AssetDatabase.LoadAssetAtPath<Material>($"{EffectsFolder}/M_FX_Additive.mat");
            var smoke = AssetDatabase.LoadAssetAtPath<Material>($"{EffectsFolder}/M_FX_Smoke.mat");
            if (additive == null || smoke == null)
            {
                Debug.LogWarning("[InteractionSetup] Run 'Create Weapon Effects' first for the explosion materials. Barrels will explode without an effect.");
                return null;
            }

            var root = new GameObject("FX_Explosion");
            try
            {
                ParticleSystem fire = WeaponEffectsSetup.AddParticles(root, additive, lifetime: 0.45f, speed: (2f, 6f), size: (0.4f, 1f),
                    burst: 25, colourA: new Color(1f, 0.9f, 0.5f), colourB: new Color(1f, 0.45f, 0.1f), coneAngle: 0f,
                    worldSpace: true, shrink: true, fade: true);
                SphereShape(fire, 0.3f);

                var smokeGo = new GameObject("Smoke");
                smokeGo.transform.SetParent(root.transform, false);
                ParticleSystem smokePs = WeaponEffectsSetup.AddParticles(smokeGo, smoke, lifetime: 1.6f, speed: (0.5f, 1.5f), size: (0.6f, 1.2f),
                    burst: 10, colourA: new Color(0.3f, 0.3f, 0.3f, 0.6f), colourB: new Color(0.15f, 0.15f, 0.15f, 0.6f), coneAngle: 0f,
                    worldSpace: true, shrink: false, fade: true);
                SphereShape(smokePs, 0.5f);
                WeaponEffectsSetup.Grow(smokePs, 2.5f);

                WeaponEffectsSetup.FinishPooled(root, autoReturnSeconds: 2f);
                return PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/FX_Explosion.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // ------------------------------------------------------------------ player & objectives

        private static bool AddPlayerComponents(ItemDefinition credits)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath) == null) return false;

            GameObject contents = PrefabUtility.LoadPrefabContents(PlayerPath);
            try
            {
                // Note: '??' doesn't work with Unity objects (a missing component is a "fake null"), so check explicitly.
                PlayerInventory inventory = contents.GetComponent<PlayerInventory>();
                if (inventory == null) inventory = contents.AddComponent<PlayerInventory>();
                SetProp(inventory, "persistentItems", p =>
                {
                    p.arraySize = 1;
                    p.GetArrayElementAtIndex(0).objectReferenceValue = credits;
                });

                PlayerInteractor interactor = contents.GetComponent<PlayerInteractor>();
                if (interactor == null) interactor = contents.AddComponent<PlayerInteractor>();
                Transform pivot = contents.transform.Find("CameraPivot");
                SetProp(interactor, "controller", p => p.objectReferenceValue = contents.GetComponent<PlayerController>());
                SetProp(interactor, "viewOrigin", p => p.objectReferenceValue = pivot);

                PrefabUtility.SaveAsPrefabAsset(contents, PlayerPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        [MenuItem("Tools/The Lost Dungeon/Add Level Objectives To Open Scene")]
        public static void AddObjectivesToScene()
        {
            if (Object.FindAnyObjectByType<LevelObjectives>() != null)
            {
                EditorUtility.DisplayDialog("Level objectives", "This scene already has a LevelObjectives object.", "OK");
                return;
            }

            PlayerController player = Object.FindAnyObjectByType<PlayerController>();
            Vector3 start = player != null ? player.transform.position : Vector3.zero;
            Vector3 forward = player != null ? player.transform.forward : Vector3.forward;

            var root = new GameObject("LevelObjectives");
            Undo.RegisterCreatedObjectUndo(root, "Add Level Objectives");
            LevelObjectives controller = root.AddComponent<LevelObjectives>();

            var defeat = new GameObject("Objective_DefeatBots");
            defeat.transform.SetParent(root.transform, false);
            DefeatEnemiesObjective defeatObjective = defeat.AddComponent<DefeatEnemiesObjective>();
            defeatObjective.EditorSetDescription("Destroy 3 security bots");
            SetProp(defeatObjective, "count", p => p.intValue = 3);

            var exit = new GameObject("Objective_ReachExit");
            exit.transform.SetParent(root.transform, false);
            exit.transform.position = start + forward * 15f;
            var zone = exit.AddComponent<BoxCollider>();
            zone.isTrigger = true;
            zone.size = new Vector3(3f, 3f, 3f);
            zone.center = new Vector3(0f, 1.5f, 0f);
            ReachZoneObjective exitObjective = exit.AddComponent<ReachZoneObjective>();
            exitObjective.EditorSetDescription("Reach the exit");

            SetProp(controller, "objectives", p =>
            {
                p.arraySize = 2;
                p.GetArrayElementAtIndex(0).objectReferenceValue = defeatObjective;
                p.GetArrayElementAtIndex(1).objectReferenceValue = exitObjective;
            });

            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeGameObject = exit;
            EditorUtility.DisplayDialog("Level objectives",
                "Added LevelObjectives with:\n1. Destroy 3 security bots\n2. Reach the exit\n\n" +
                "The exit zone (selected) was placed 15 m in front of the player. Move it to the real exit.",
                "OK");
        }

        // ------------------------------------------------------------------ helpers

        private static GameObject InstantiateModel(string modelName, Transform parent)
        {
            string path = $"{ModelFolder}/{modelName}.fbx";
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model == null) throw new FileNotFoundException($"Model not found: {path}");

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            instance.transform.SetParent(parent, false);
            foreach (Collider c in instance.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            return instance;
        }

        /// <summary>Box collider on the root that wraps all renderers.</summary>
        private static void FitBoxCollider(GameObject root, bool isTrigger)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);

            var box = root.AddComponent<BoxCollider>();
            box.isTrigger = isTrigger;
            box.center = root.transform.InverseTransformPoint(bounds.center);
            box.size = bounds.size;
        }

        private static void GlowLight(Transform parent, Color colour)
        {
            var go = new GameObject("Glow");
            go.transform.SetParent(parent, false);
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = colour;
            light.range = 1.6f;
            light.intensity = 1.5f;
            light.shadows = LightShadows.None;
        }

        private static GameObject Cube(Transform parent, string name, Vector3 position, Vector3 size, Material material, bool keepCollider)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            if (!keepCollider) Object.DestroyImmediate(cube.GetComponent<Collider>());
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = position;
            cube.transform.localScale = size;
            var renderer = cube.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return cube;
        }

        private static void SphereShape(ParticleSystem ps, float radius)
        {
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;
        }

        private static Transform Child(Transform parent, string name, Vector3 localPosition)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = localPosition;
            return t;
        }

        private static Transform FindChild(Transform root, string exactName) =>
            root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == exactName);

        private static Transform FindChildContaining(Transform root, string part) =>
            root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t != root && t.name.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0);

        private static GameObject Save(GameObject root, string name) =>
            PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabFolder}/{name}.prefab");

        private static void SetProp(Object target, string property, Action<SerializedProperty> set)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(property);
            if (prop == null)
            {
                Debug.LogWarning($"[InteractionSetup] {target.GetType().Name} has no field '{property}'.");
                return;
            }
            set(prop);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static ItemDefinition Item(string assetName, string id, string displayName, int maxStack, bool persistent)
        {
            string path = $"{ItemFolder}/{assetName}.asset";
            var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (item != null) return item;

            item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.EditorInitialise(id, displayName, maxStack, persistent);
            AssetDatabase.CreateAsset(item, path);
            return item;
        }

        private static LootTable LootTableAsset(string assetName, List<LootTable.Entry> entries)
        {
            string path = $"{LootFolder}/{assetName}.asset";
            var table = AssetDatabase.LoadAssetAtPath<LootTable>(path);
            if (table == null)
            {
                table = ScriptableObject.CreateInstance<LootTable>();
                AssetDatabase.CreateAsset(table, path);
            }
            table.EditorSetEntries(entries);
            EditorUtility.SetDirty(table);
            return table;
        }

        private static Material Lit(string name, Color colour, float metallic, float smoothness, Color? emission)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", colour);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Smoothness", smoothness);
            if (emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
