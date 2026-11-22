using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TheLostDungeon.Combat;
using TheLostDungeon.Enemies;
using TheLostDungeon.Interaction;
using TheLostDungeon.Items;
using TheLostDungeon.Pooling;
using TheLostDungeon.Weapons;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TheLostDungeon.EditorTools
{
    /// <summary>
    /// Tools > The Lost Dungeon > Create Enemy Prefabs
    ///     Sets the Quaternius enemy models to Generic rigs with looping clips, creates the three
    ///     Enemy Profiles, an enemy bolt, the Warrior / Archer / Guardian prefabs, an EnemySpawner and
    ///     an ActivationZone. Also prepares doors (carve the NavMesh while closed) and keeps the
    ///     player and pickups out of NavMesh baking.
    ///
    /// Tools > The Lost Dungeon > Build Enemy Animator Controllers (optional)
    ///     Builds AC_Enemy (Locomotion blend tree, Attack, Hit, Death; parameters Speed / Attack / Hit
    ///     / Dead) plus override controllers for the Guardian and Archer, and assigns them.
    ///     Skip this if you'd rather build the controllers by hand in the Animator window.
    ///
    /// Tools > The Lost Dungeon > Add NavMesh Surface To Open Scene
    ///     Adds a configured NavMeshSurface. Then press "Bake" on it.
    /// </summary>
    public static class EnemySetup
    {
        private const string ModelFolder = "Assets/Quaternius";
        private const string PrefabFolder = "Assets/Prefabs/Enemies";
        private const string ProfileFolder = "Assets/Data/Enemies";
        private const string AnimFolder = "Assets/Animations/Enemies";
        private const string MaterialFolder = "Assets/Prefabs/Materials";

        private const string WarriorModel = ModelFolder + "/Enemy_Trilobite.fbx";
        private const string ArcherModel = ModelFolder + "/Enemy_EyeDrone.fbx";
        private const string GuardianModel = ModelFolder + "/Enemy_QuadShell.fbx";

        private const string BaseControllerPath = AnimFolder + "/AC_Enemy.controller";
        private const string GuardianOverridePath = AnimFolder + "/AOC_Guardian.overrideController";
        private const string ArcherOverridePath = AnimFolder + "/AOC_Archer.overrideController";

        private static readonly Regex LoopingClips = new Regex("Idle|Walk|Run|Look|Charging|Hanging", RegexOptions.IgnoreCase);

        // ------------------------------------------------------------------ enemies

        [MenuItem("Tools/The Lost Dungeon/Create Enemy Prefabs")]
        public static void CreateEnemyPrefabs()
        {
            EnsureFolder(PrefabFolder);
            EnsureFolder(ProfileFolder);
            EnsureFolder(MaterialFolder);

            foreach (string model in new[] { WarriorModel, ArcherModel, GuardianModel }) ConfigureModel(model);

            EnemyProfile warrior = Profile("EP_Warrior", p =>
            {
                p.enemyId = "warrior_bot"; p.displayName = "Warrior"; p.scoreValue = 100;
                p.maxHealth = 60f; p.walkSpeed = 1.8f; p.runSpeed = 4.5f; p.wanderRadius = 5f;
                p.sightRange = 16f; p.sightAngle = 120f; p.hearingRange = 25f; p.eyeHeight = 0.8f;
                p.preferredMinDistance = 0f; p.attackRange = 1.8f; p.attackCooldown = 1.1f;
            });
            EnemyProfile archer = Profile("EP_Archer", p =>
            {
                p.enemyId = "archer_drone"; p.displayName = "Archer Drone"; p.scoreValue = 150;
                p.maxHealth = 40f; p.walkSpeed = 2f; p.runSpeed = 3.5f; p.wanderRadius = 6f;
                p.sightRange = 22f; p.sightAngle = 140f; p.hearingRange = 25f; p.eyeHeight = 1.5f;   // hovers
                p.preferredMinDistance = 7f; p.attackRange = 14f; p.attackCooldown = 1.6f;
            });
            EnemyProfile guardian = Profile("EP_Guardian", p =>
            {
                p.enemyId = "guardian_bot"; p.displayName = "Guardian"; p.scoreValue = 250;
                p.maxHealth = 160f; p.defence = DefenceProfile.Shielded(0.9f, 120f);
                p.staggerSeconds = 1.6f; p.walkSpeed = 1.2f; p.runSpeed = 2.4f; p.wanderRadius = 2f; p.leashRadius = 10f;
                p.sightRange = 14f; p.sightAngle = 100f; p.hearingRange = 18f; p.eyeHeight = 1f;
                p.preferredMinDistance = 0f; p.attackRange = 2.4f; p.attackCooldown = 2f;
            });

            GameObject bolt = EnemyBoltPrefab();
            var loot = AssetDatabase.LoadAssetAtPath<LootTable>("Assets/Data/Loot/LT_Chest.asset");

            GameObject warriorPrefab = EnemyPrefab("Enemy_Warrior", WarriorModel, warrior, hoverHeight: 0f, loot, 0.35f, root =>
            {
                MeleeAttack a = root.AddComponent<MeleeAttack>();
                SetProp(a, "damage", p => p.floatValue = 12f);
                return a;
            });
            GameObject archerPrefab = EnemyPrefab("Enemy_Archer", ArcherModel, archer, hoverHeight: 1.2f, loot, 0.4f, root =>
            {
                RangedAttack a = root.AddComponent<RangedAttack>();
                Transform muzzle = Child(root.transform, "Muzzle", new Vector3(0f, archer.eyeHeight, 0.5f));
                SetProp(a, "projectilePrefab", p => p.objectReferenceValue = bolt);
                SetProp(a, "muzzle", p => p.objectReferenceValue = muzzle);
                return a;
            });
            GameObject guardianPrefab = EnemyPrefab("Enemy_Guardian", GuardianModel, guardian, hoverHeight: 0f, loot, 0.8f, root =>
            {
                ShieldBashAttack a = root.AddComponent<ShieldBashAttack>();
                SetProp(a, "damage", p => p.floatValue = 28f);
                SetProp(a, "reach", p => p.floatValue = 2.6f);
                SetProp(a, "windUpSeconds", p => p.floatValue = 0.5f);
                SetProp(a, "knockback", p => p.floatValue = 8f);
                return a;
            });

            SpawnerPrefab(warriorPrefab, archerPrefab);
            ActivationZonePrefab();
            PrepareDoorsAndIgnoredObjects();

            AssignControllersIfPresent();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Enemies",
                "Created in " + PrefabFolder + ":\n• Enemy_Warrior, Enemy_Archer, Enemy_Guardian\n• EnemySpawner, ActivationZone\n\n" +
                "Profiles in " + ProfileFolder + " (tune health, speed, ranges there).\n\n" +
                "Next:\n1. Animator controllers: build them by hand, or use 'Build Enemy Animator Controllers'.\n" +
                "2. In each level: 'Add NavMesh Surface To Open Scene', then press Bake.\n" +
                "3. Drag enemies into corridors and rooms.",
                "OK");
        }

        private static GameObject EnemyPrefab(string name, string modelPath, EnemyProfile profile, float hoverHeight,
                                              LootTable loot, float dropChance, Func<GameObject, EnemyAttack> addAttack)
        {
            var root = new GameObject(name);
            try
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath));
                model.transform.SetParent(root.transform, false);
                model.transform.localPosition = new Vector3(0f, hoverHeight, 0f);
                foreach (Collider c in model.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);

                Bounds bounds = RendererBounds(root);
                float radius = Mathf.Clamp(Mathf.Max(bounds.extents.x, bounds.extents.z), 0.3f, 0.9f);
                float height = Mathf.Max(0.6f, bounds.max.y - root.transform.position.y);

                var capsule = root.AddComponent<CapsuleCollider>();
                capsule.radius = radius;
                capsule.height = Mathf.Max(height, radius * 2f);
                capsule.center = new Vector3(0f, capsule.height * 0.5f, 0f);

                var agent = root.AddComponent<NavMeshAgent>();
                agent.radius = radius;
                agent.height = Mathf.Max(1f, height);
                agent.speed = profile.walkSpeed;
                agent.angularSpeed = 360f;
                agent.acceleration = 12f;
                agent.stoppingDistance = 0.3f;

                Health health = root.AddComponent<Health>();
                SetProp(health, "maxHealth", p => p.floatValue = profile.maxHealth);
                root.AddComponent<DamageFlash>();

                Animator animator = model.GetComponent<Animator>();
                if (animator == null) animator = model.AddComponent<Animator>();
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

                var voice = root.AddComponent<AudioSource>();
                voice.playOnAwake = false;
                voice.spatialBlend = 1f;
                voice.maxDistance = 25f;

                EnemyAttack attack = addAttack(root);

                EnemyBrain brain = root.AddComponent<EnemyBrain>();
                SetProp(brain, "profile", p => p.objectReferenceValue = profile);
                SetProp(brain, "attack", p => p.objectReferenceValue = attack);
                SetProp(brain, "animator", p => p.objectReferenceValue = animator);
                SetProp(brain, "voice", p => p.objectReferenceValue = voice);

                EnemyLoot enemyLoot = root.AddComponent<EnemyLoot>();
                SetProp(enemyLoot, "lootTable", p => p.objectReferenceValue = loot);
                SetProp(enemyLoot, "dropChance", p => p.floatValue = dropChance);

                root.AddComponent<PooledObject>();
                var modifier = root.AddComponent<NavMeshModifier>();
                modifier.ignoreFromBuild = true;

                return PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabFolder}/{name}.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject EnemyBoltPrefab()
        {
            Color red = new Color(1f, 0.25f, 0.2f);
            Material core = LitEmissive("M_EnemyBolt", red, red * 4f);
            var trailMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/Effects/M_FX_Additive.mat");

            var root = new GameObject("Projectile_EnemyBolt");
            try
            {
                root.AddComponent<PooledObject>();
                root.AddComponent<Projectile>();

                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.name = "Core";
                Object.DestroyImmediate(sphere.GetComponent<Collider>());
                sphere.transform.SetParent(root.transform, false);
                sphere.transform.localScale = new Vector3(0.12f, 0.12f, 0.35f);
                var r = sphere.GetComponent<MeshRenderer>();
                r.sharedMaterial = core;
                r.shadowCastingMode = ShadowCastingMode.Off;

                var trail = root.AddComponent<TrailRenderer>();
                trail.time = 0.12f;
                trail.startWidth = 0.08f;
                trail.endWidth = 0f;
                trail.startColor = red;
                trail.endColor = new Color(red.r, red.g, red.b, 0f);
                if (trailMat != null) trail.sharedMaterial = trailMat;
                trail.shadowCastingMode = ShadowCastingMode.Off;

                return PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabFolder}/Projectile_EnemyBolt.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void SpawnerPrefab(GameObject warrior, GameObject archer)
        {
            var root = new GameObject("EnemySpawner");
            try
            {
                Transform a = Child(root.transform, "SpawnPoint_A", new Vector3(-1.5f, 0f, 0f));
                Transform b = Child(root.transform, "SpawnPoint_B", new Vector3(1.5f, 0f, 0f));
                EnemySpawner spawner = root.AddComponent<EnemySpawner>();
                SetProp(spawner, "enemyPrefabs", p =>
                {
                    p.arraySize = 3;
                    p.GetArrayElementAtIndex(0).objectReferenceValue = warrior;
                    p.GetArrayElementAtIndex(1).objectReferenceValue = warrior;
                    p.GetArrayElementAtIndex(2).objectReferenceValue = archer;
                });
                SetProp(spawner, "spawnPoints", p =>
                {
                    p.arraySize = 2;
                    p.GetArrayElementAtIndex(0).objectReferenceValue = a;
                    p.GetArrayElementAtIndex(1).objectReferenceValue = b;
                });
                PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabFolder}/EnemySpawner.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void ActivationZonePrefab()
        {
            var root = new GameObject("ActivationZone");
            try
            {
                var box = root.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(4f, 3f, 4f);
                box.center = new Vector3(0f, 1.5f, 0f);
                root.AddComponent<ActivationZone>();
                PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabFolder}/ActivationZone.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// Closed door panels carve a hole in the NavMesh (enemies can't path through) and stop
        /// carving once the panel sinks away. The player and pickups are left out of the bake.
        /// </summary>
        private static void PrepareDoorsAndIgnoredObjects()
        {
            string[] prefabPaths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" })
                .Select(AssetDatabase.GUIDToAssetPath).ToArray();

            foreach (string path in prefabPaths)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset == null) continue;

                bool isDoor = asset.GetComponent<Door>() != null;
                bool ignore = asset.GetComponent<Pickup>() != null || asset.GetComponent<TheLostDungeon.Player.PlayerController>() != null;
                if (!isDoor && !ignore) continue;

                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (isDoor)
                    {
                        Transform body = contents.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "PanelBody");
                        if (body != null)
                        {
                            NavMeshObstacle obstacle = body.GetComponent<NavMeshObstacle>();
                            if (obstacle == null) obstacle = body.gameObject.AddComponent<NavMeshObstacle>();
                            obstacle.shape = NavMeshObstacleShape.Box;
                            obstacle.center = Vector3.zero;
                            obstacle.size = Vector3.one;
                            obstacle.carving = true;

                            NavMeshModifier m = body.parent.GetComponent<NavMeshModifier>();
                            if (m == null) m = body.parent.gameObject.AddComponent<NavMeshModifier>();
                            m.ignoreFromBuild = true;
                        }
                    }
                    else
                    {
                        NavMeshModifier m = contents.GetComponent<NavMeshModifier>();
                        if (m == null) m = contents.AddComponent<NavMeshModifier>();
                        m.ignoreFromBuild = true;
                    }

                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }
        }

        // ------------------------------------------------------------------ animator controllers

        [MenuItem("Tools/The Lost Dungeon/Build Enemy Animator Controllers (optional)")]
        public static void BuildAnimatorControllers()
        {
            EnsureFolder(AnimFolder);
            foreach (string model in new[] { WarriorModel, ArcherModel, GuardianModel }) ConfigureModel(model);

            // Base controller built from the Warrior's clips.
            var roles = new Dictionary<string, AnimationClip>
            {
                ["Idle"] = Clip(WarriorModel, "Idle"),
                ["Walk"] = Clip(WarriorModel, "Walk"),
                ["Run"] = Clip(WarriorModel, "Run"),
                ["Attack"] = Clip(WarriorModel, "Attack"),
                ["Hit"] = Clip(WarriorModel, "Hit"),
                ["Death"] = Clip(WarriorModel, "TurnOff"),
            };
            if (roles.Values.Any(c => c == null))
            {
                EditorUtility.DisplayDialog("Animator", "Couldn't find all Warrior clips (Idle, Walk, Run, Attack, Hit, TurnOff).", "OK");
                return;
            }

            AssetDatabase.DeleteAsset(BaseControllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(BaseControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Dead", AnimatorControllerParameterType.Bool);

            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            AnimatorState locomotion = controller.CreateBlendTreeInController("Locomotion", out BlendTree tree, 0);
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            tree.AddChild(roles["Idle"], 0f);
            tree.AddChild(roles["Walk"], 1.5f);
            tree.AddChild(roles["Run"], 4f);
            sm.defaultState = locomotion;

            AnimatorState attack = sm.AddState("Attack");
            attack.motion = roles["Attack"];
            AnimatorState hit = sm.AddState("Hit");
            hit.motion = roles["Hit"];
            AnimatorState death = sm.AddState("Death");
            death.motion = roles["Death"];

            AnyStateTo(sm, attack, "Attack", AnimatorConditionMode.If);
            AnyStateTo(sm, hit, "Hit", AnimatorConditionMode.If);
            AnimatorStateTransition toDeath = sm.AddAnyStateTransition(death);
            toDeath.AddCondition(AnimatorConditionMode.If, 0f, "Dead");
            toDeath.duration = 0.1f;
            toDeath.canTransitionToSelf = false;
            toDeath.hasExitTime = false;

            BackToLocomotion(attack, locomotion, 0.9f);
            BackToLocomotion(hit, locomotion, 0.8f);

            BuildOverride(controller, roles, GuardianOverridePath, new Dictionary<string, AnimationClip>
            {
                ["Idle"] = Clip(GuardianModel, "Idle"),
                ["Walk"] = Clip(GuardianModel, "Walk"),
                ["Run"] = Clip(GuardianModel, "Run"),
                ["Attack"] = Clip(GuardianModel, "Attack"),
                ["Hit"] = Clip(GuardianModel, "Hit"),
                ["Death"] = Clip(GuardianModel, "TurnOff"),
            });
            BuildOverride(controller, roles, ArcherOverridePath, new Dictionary<string, AnimationClip>
            {
                // The drone hovers: Idle doubles as Walk/Run, and it has no death clip (it sinks instead).
                ["Idle"] = Clip(ArcherModel, "Idle"),
                ["Walk"] = Clip(ArcherModel, "Idle"),
                ["Run"] = Clip(ArcherModel, "Idle"),
                ["Attack"] = Clip(ArcherModel, "Attack"),
                ["Hit"] = Clip(ArcherModel, "Hit"),
                ["Death"] = Clip(ArcherModel, "Hit"),
            });

            AssetDatabase.SaveAssets();
            AssignControllersIfPresent();

            EditorUtility.DisplayDialog("Animator",
                "Built in " + AnimFolder + ":\n• AC_Enemy (Warrior)\n• AOC_Guardian, AOC_Archer (override controllers)\n\n" +
                "Open AC_Enemy in Window > Animation > Animator to see the states and transitions.",
                "OK");
        }

        private static void AnyStateTo(AnimatorStateMachine sm, AnimatorState state, string trigger, AnimatorConditionMode mode)
        {
            AnimatorStateTransition t = sm.AddAnyStateTransition(state);
            t.AddCondition(mode, 0f, trigger);
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, "Dead");   // never interrupt death
            t.duration = 0.1f;
            t.canTransitionToSelf = false;
            t.hasExitTime = false;
        }

        private static void BackToLocomotion(AnimatorState from, AnimatorState locomotion, float exitTime)
        {
            AnimatorStateTransition t = from.AddTransition(locomotion);
            t.hasExitTime = true;
            t.exitTime = exitTime;
            t.duration = 0.15f;
        }

        private static void BuildOverride(AnimatorController baseController, Dictionary<string, AnimationClip> baseRoles,
                                          string path, Dictionary<string, AnimationClip> replacements)
        {
            AssetDatabase.DeleteAsset(path);
            var aoc = new AnimatorOverrideController(baseController);
            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            aoc.GetOverrides(overrides);

            for (int i = 0; i < overrides.Count; i++)
            {
                AnimationClip original = overrides[i].Key;
                string role = baseRoles.FirstOrDefault(r => r.Value == original).Key;
                if (role != null && replacements.TryGetValue(role, out AnimationClip replacement) && replacement != null)
                {
                    overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(original, replacement);
                }
            }

            aoc.ApplyOverrides(overrides);
            AssetDatabase.CreateAsset(aoc, path);
        }

        private static void AssignControllersIfPresent()
        {
            AssignController($"{PrefabFolder}/Enemy_Warrior.prefab", BaseControllerPath);
            AssignController($"{PrefabFolder}/Enemy_Guardian.prefab", GuardianOverridePath);
            AssignController($"{PrefabFolder}/Enemy_Archer.prefab", ArcherOverridePath);
        }

        private static void AssignController(string prefabPath, string controllerPath)
        {
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
            if (controller == null || AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null) return;

            GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                Animator animator = contents.GetComponentInChildren<Animator>();
                if (animator == null) return;
                animator.runtimeAnimatorController = controller;
                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        // ------------------------------------------------------------------ navmesh

        [MenuItem("Tools/The Lost Dungeon/Add NavMesh Surface To Open Scene")]
        public static void AddNavMeshSurface()
        {
            NavMeshSurface surface = Object.FindAnyObjectByType<NavMeshSurface>();
            if (surface == null)
            {
                var go = new GameObject("NavMesh");
                Undo.RegisterCreatedObjectUndo(go, "Add NavMesh Surface");
                surface = go.AddComponent<NavMeshSurface>();
            }

            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;   // triggers (zones) don't cut holes
            EditorSceneManager.MarkSceneDirty(surface.gameObject.scene);
            Selection.activeGameObject = surface.gameObject;

            EditorUtility.DisplayDialog("NavMesh",
                "Added a NavMeshSurface (selected). Press 'Bake' on it in the Inspector, then save the scene.\n\n" +
                "Blue areas in the Scene view show where enemies can walk. Re-bake whenever you move walls or crates.",
                "OK");
        }

        // ------------------------------------------------------------------ helpers

        private static void ConfigureModel(string path)
        {
            if (!(AssetImporter.GetAtPath(path) is ModelImporter importer)) return;

            importer.animationType = ModelImporterAnimationType.Generic;
            ModelImporterClipAnimation[] clips = importer.clipAnimations.Length > 0 ? importer.clipAnimations : importer.defaultClipAnimations;
            foreach (ModelImporterClipAnimation clip in clips) clip.loopTime = LoopingClips.IsMatch(clip.name);
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static AnimationClip Clip(string modelPath, string name) =>
            AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview", StringComparison.Ordinal))
                .FirstOrDefault(c => c.name == name || c.name.EndsWith("|" + name, StringComparison.Ordinal));

        private static EnemyProfile Profile(string assetName, Action<EnemyProfile> configure)
        {
            string path = $"{ProfileFolder}/{assetName}.asset";
            var profile = AssetDatabase.LoadAssetAtPath<EnemyProfile>(path);
            if (profile != null) return profile;   // keep your tuning on re-runs

            profile = ScriptableObject.CreateInstance<EnemyProfile>();
            configure(profile);
            AssetDatabase.CreateAsset(profile, path);
            return profile;
        }

        private static Bounds RendererBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(root.transform.position + Vector3.up, Vector3.one);
            Bounds b = renderers[0].bounds;
            foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
            return b;
        }

        private static Transform Child(Transform parent, string name, Vector3 localPosition)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = localPosition;
            return t;
        }

        private static void SetProp(Object target, string property, Action<SerializedProperty> set)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(property);
            if (prop == null)
            {
                Debug.LogWarning($"[EnemySetup] {target.GetType().Name} has no field '{property}'.");
                return;
            }
            set(prop);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Material LitEmissive(string name, Color colour, Color emission)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", colour);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emission);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
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
