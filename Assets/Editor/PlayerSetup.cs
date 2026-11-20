using System;
using System.IO;
using System.Linq;
using TheLostDungeon.Combat;
using TheLostDungeon.Player;
using TheLostDungeon.Pooling;
using TheLostDungeon.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TheLostDungeon.EditorTools
{
    /// <summary>
    /// Tools > The Lost Dungeon > Create Player Prefabs
    ///     Builds the Player (camera, blaster, melee, health), the pooled bolt / spark / muzzle-flash
    ///     prefabs and two training dummies, all wired up, in Assets/Prefabs.
    ///     The gun is made of simple boxes so it works immediately. Swap in a real model later by
    ///     placing it under Player/CameraPivot/WeaponHolder/BlasterModel.
    ///
    /// Tools > The Lost Dungeon > Replace Demo Player In Level Scenes
    ///     Opens Level_01..Level_05, removes the warehouse kit's FirstPersonPlayer (and its camera),
    ///     puts the new Player in the same spot and adds training dummies to Level_01.
    /// </summary>
    public static class PlayerSetup
    {
        private const string PrefabFolder = "Assets/Prefabs";
        private const string MaterialFolder = PrefabFolder + "/Materials";
        private const string InputAssetPath = "Assets/InputSystem_Actions.inputactions";

        private const string PlayerPath = PrefabFolder + "/Player.prefab";
        private const string BoltPath = PrefabFolder + "/Projectile_Bolt.prefab";
        private const string ImpactPath = PrefabFolder + "/FX_ImpactSpark.prefab";
        private const string FlashPath = PrefabFolder + "/FX_MuzzleFlash.prefab";
        private const string DummyPath = PrefabFolder + "/TrainingDummy.prefab";
        private const string ShieldDummyPath = PrefabFolder + "/TrainingDummy_Shielded.prefab";

        private static readonly Color Energy = new Color(0.25f, 0.85f, 1f);

        // ------------------------------------------------------------------ prefabs

        [MenuItem("Tools/The Lost Dungeon/Create Player Prefabs")]
        public static void CreatePlayerPrefabs()
        {
            var controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
            if (controls == null)
            {
                EditorUtility.DisplayDialog("The Lost Dungeon", $"Couldn't find {InputAssetPath}.", "OK");
                return;
            }

            EnsureFolder(MaterialFolder);

            Material gunMat = LitMaterial("M_Gun", new Color(0.18f, 0.2f, 0.23f), metallic: 0.6f, smoothness: 0.55f);
            Material energyMat = LitMaterial("M_Energy", Energy, emission: Energy * 3f);
            Material fxMat = UnlitParticleMaterial("M_FX_Energy", Energy);
            Material dummyMat = LitMaterial("M_Dummy", new Color(0.95f, 0.55f, 0.15f), smoothness: 0.3f);
            Material shieldMat = LitMaterial("M_Shield", new Color(0.3f, 0.6f, 1f), emission: new Color(0.1f, 0.3f, 0.6f));

            GameObject impact = SaveParticlePrefab(ImpactPath, "FX_ImpactSpark", fxMat, lifetime: 0.25f, speedMin: 2f, speedMax: 5f,
                                                    sizeMin: 0.03f, sizeMax: 0.07f, burst: 14, coneAngle: 35f, gravity: 0.6f, autoReturn: 0.6f);
            GameObject flash = SaveParticlePrefab(FlashPath, "FX_MuzzleFlash", fxMat, lifetime: 0.05f, speedMin: 0f, speedMax: 0.5f,
                                                   sizeMin: 0.12f, sizeMax: 0.18f, burst: 3, coneAngle: 10f, gravity: 0f, autoReturn: 0.2f);
            GameObject bolt = SaveBoltPrefab(energyMat, fxMat);
            SavePlayerPrefab(controls, bolt, impact, flash, gunMat, energyMat);
            SaveDummyPrefab(DummyPath, "TrainingDummy", dummyMat, null, shielded: false);
            SaveDummyPrefab(ShieldDummyPath, "TrainingDummy_Shielded", dummyMat, shieldMat, shielded: true);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("The Lost Dungeon",
                "Created in " + PrefabFolder + ":\n\n" +
                "• Player (camera, blaster, melee, health)\n" +
                "• Projectile_Bolt, FX_ImpactSpark, FX_MuzzleFlash (pooled)\n" +
                "• TrainingDummy and TrainingDummy_Shielded\n\n" +
                "Next: Tools > The Lost Dungeon > Replace Demo Player In Level Scenes.",
                "OK");
        }

        private static void SavePlayerPrefab(InputActionAsset controls, GameObject bolt, GameObject impact, GameObject flash,
                                             Material gunMat, Material energyMat)
        {
            var root = new GameObject("Player");
            try
            {
                var cc = root.AddComponent<CharacterController>();
                cc.height = 1.8f;
                cc.radius = 0.35f;
                cc.center = new Vector3(0f, 0.9f, 0f);
                cc.stepOffset = 0.4f;

                Health health = root.AddComponent<Health>();
                Set(health, "maxHealth", 100f);
                Set(health, "invulnerabilitySeconds", 0.25f);
                root.AddComponent<PlayerHealthBridge>();

                // Camera
                Transform pivot = Child(root.transform, "CameraPivot", new Vector3(0f, 1.6f, 0f));
                pivot.gameObject.tag = "MainCamera";
                var cam = pivot.gameObject.AddComponent<Camera>();
                cam.nearClipPlane = 0.03f;
                cam.fieldOfView = 70f;
                pivot.gameObject.AddComponent<AudioListener>();

                // First-person gun made of boxes (replace with a real model later)
                Transform holder = Child(pivot, "WeaponHolder", new Vector3(0.22f, -0.2f, 0.4f));
                Transform model = Child(holder, "BlasterModel", Vector3.zero);
                Box(model, "Body", new Vector3(0f, 0f, 0f), new Vector3(0.08f, 0.1f, 0.32f), gunMat);
                Box(model, "Barrel", new Vector3(0f, 0.02f, 0.22f), new Vector3(0.045f, 0.045f, 0.16f), gunMat);
                Box(model, "Grip", new Vector3(0f, -0.09f, -0.07f), new Vector3(0.05f, 0.12f, 0.06f), gunMat);
                Box(model, "EnergyCell", new Vector3(0f, 0.065f, -0.02f), new Vector3(0.05f, 0.03f, 0.14f), energyMat);
                Transform muzzle = Child(model, "Muzzle", new Vector3(0f, 0.02f, 0.31f));

                BlasterWeapon blaster = holder.gameObject.AddComponent<BlasterWeapon>();
                Set(blaster, "muzzle", muzzle);
                Set(blaster, "projectilePrefab", bolt);
                Set(blaster, "impactEffectPrefab", impact);
                Set(blaster, "muzzleFlashPrefab", flash);

                MeleeWeapon melee = holder.gameObject.AddComponent<MeleeWeapon>();

                PlayerController controller = root.AddComponent<PlayerController>();
                Set(controller, "controls", controls);
                Set(controller, "cameraPivot", pivot);
                Set(controller, "health", health);

                WeaponViewModel viewModel = holder.gameObject.AddComponent<WeaponViewModel>();
                Set(viewModel, "model", model);
                Set(viewModel, "blaster", blaster);
                Set(viewModel, "melee", melee);
                Set(viewModel, "player", controller);

                PlayerCombat combat = root.AddComponent<PlayerCombat>();
                Set(combat, "controller", controller);
                Set(combat, "aimOrigin", pivot);
                Set(combat, "primaryWeapon", blaster);
                Set(combat, "secondaryWeapon", melee);

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject SaveBoltPrefab(Material energyMat, Material trailMat)
        {
            var root = new GameObject("Projectile_Bolt");
            try
            {
                root.AddComponent<PooledObject>();
                root.AddComponent<Projectile>();

                GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                core.name = "Core";
                Object.DestroyImmediate(core.GetComponent<Collider>());
                core.transform.SetParent(root.transform, false);
                core.transform.localScale = new Vector3(0.07f, 0.07f, 0.28f);
                var coreRenderer = core.GetComponent<MeshRenderer>();
                coreRenderer.sharedMaterial = energyMat;
                coreRenderer.shadowCastingMode = ShadowCastingMode.Off;

                var trail = root.AddComponent<TrailRenderer>();
                trail.time = 0.08f;
                trail.startWidth = 0.05f;
                trail.endWidth = 0f;
                trail.minVertexDistance = 0.05f;
                trail.sharedMaterial = trailMat;
                trail.shadowCastingMode = ShadowCastingMode.Off;

                return PrefabUtility.SaveAsPrefabAsset(root, BoltPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject SaveParticlePrefab(string path, string name, Material material, float lifetime,
                                                     float speedMin, float speedMax, float sizeMin, float sizeMax,
                                                     int burst, float coneAngle, float gravity, float autoReturn)
        {
            var root = new GameObject(name);
            try
            {
                var ps = root.AddComponent<ParticleSystem>();
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                ParticleSystem.MainModule main = ps.main;
                main.duration = Mathf.Max(0.05f, lifetime);
                main.loop = false;
                main.playOnAwake = false;     // PooledParticleEffect plays it on spawn
                main.startLifetime = lifetime;
                main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
                main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
                main.startColor = Energy;
                main.gravityModifier = gravity;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.maxParticles = Mathf.Max(burst * 2, 10);

                ParticleSystem.EmissionModule emission = ps.emission;
                emission.rateOverTime = 0f;
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burst) });

                ParticleSystem.ShapeModule shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = coneAngle;
                shape.radius = 0.02f;

                var renderer = root.GetComponent<ParticleSystemRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;

                root.AddComponent<PooledParticleEffect>();
                PooledObject pooled = root.AddComponent<PooledObject>();
                Set(pooled, "autoReturnSeconds", autoReturn);

                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void SaveDummyPrefab(string path, string name, Material bodyMat, Material shieldMat, bool shielded)
        {
            var root = new GameObject(name);
            try
            {
                GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.name = "Body";
                body.transform.SetParent(root.transform, false);
                body.transform.localPosition = new Vector3(0f, 1f, 0f);
                body.GetComponent<MeshRenderer>().sharedMaterial = bodyMat;

                GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Cube);
                eye.name = "Visor";
                Object.DestroyImmediate(eye.GetComponent<Collider>());
                eye.transform.SetParent(root.transform, false);
                eye.transform.localPosition = new Vector3(0f, 1.5f, 0.42f);
                eye.transform.localScale = new Vector3(0.5f, 0.12f, 0.1f);
                eye.GetComponent<MeshRenderer>().sharedMaterial = shieldMat != null ? shieldMat : bodyMat;

                Transform aim = Child(root.transform, "AimPoint", new Vector3(0f, 1.2f, 0f));

                Health health = root.AddComponent<Health>();
                Set(health, "maxHealth", 120f);

                if (shielded)
                {
                    GameObject shield = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    shield.name = "Shield";
                    shield.transform.SetParent(root.transform, false);
                    shield.transform.localPosition = new Vector3(0f, 1f, 0.65f);
                    shield.transform.localScale = new Vector3(1.2f, 1.6f, 0.1f);
                    shield.GetComponent<MeshRenderer>().sharedMaterial = shieldMat;

                    var so = new SerializedObject(health);
                    so.FindProperty("defence.hasFrontShield").boolValue = true;
                    so.FindProperty("defence.shieldBlockFraction").floatValue = 0.9f;
                    so.FindProperty("defence.shieldArcDegrees").floatValue = 120f;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                TrainingTarget target = root.AddComponent<TrainingTarget>();
                Set(target, "aimPoint", aim);
                Set(target, "enemyId", shielded ? "training_dummy_shielded" : "training_dummy");
                root.AddComponent<DamageFlash>();

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // ------------------------------------------------------------------ scene replacement

        [MenuItem("Tools/The Lost Dungeon/Replace Demo Player In Level Scenes")]
        public static void ReplaceDemoPlayer()
        {
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath);
            var dummyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DummyPath);
            var shieldDummyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShieldDummyPath);
            if (playerPrefab == null)
            {
                EditorUtility.DisplayDialog("The Lost Dungeon", "Run Tools > The Lost Dungeon > Create Player Prefabs first.", "OK");
                return;
            }

            bool proceed = EditorUtility.DisplayDialog("Replace demo player",
                "This opens Level_01 to Level_05, removes the warehouse kit's FirstPersonPlayer (and its camera), " +
                "adds the new Player in the same spot, adds two training dummies to Level_01, and saves the scenes.\n\n" +
                "Commit your work first so you can undo it with Git if needed.",
                "Replace", "Cancel");
            if (!proceed || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            string originalScene = SceneManager.GetActiveScene().path;
            string[] scenePaths = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => Path.GetFileNameWithoutExtension(p).StartsWith("Level_", StringComparison.Ordinal))
                .OrderBy(p => p)
                .ToArray();

            var report = new System.Text.StringBuilder();

            foreach (string path in scenePaths)
            {
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                string sceneName = Path.GetFileNameWithoutExtension(path);

                if (Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0)
                {
                    report.AppendLine($"{sceneName}: already has the new Player, skipped.");
                    continue;
                }

                Vector3 position = Vector3.zero;
                Quaternion rotation = Quaternion.identity;
                GameObject demo = FindInScene(scene, "FirstPersonPlayer");
                if (demo != null)
                {
                    // The demo player's pivot is at its middle; ours is at the feet.
                    position = demo.transform.position - new Vector3(0f, 0.9f, 0f);
                    rotation = Quaternion.Euler(0f, demo.transform.eulerAngles.y, 0f);
                    Object.DestroyImmediate(demo);
                    report.AppendLine($"{sceneName}: replaced FirstPersonPlayer.");
                }
                else
                {
                    report.AppendLine($"{sceneName}: no FirstPersonPlayer found, Player placed at the origin. Move it.");
                }

                var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
                player.transform.SetPositionAndRotation(position, rotation);

                if (sceneName == "Level_01")
                {
                    Vector3 forward = rotation * Vector3.forward;
                    Vector3 right = rotation * Vector3.right;
                    Quaternion facePlayer = Quaternion.LookRotation(-forward);
                    if (dummyPrefab != null)
                    {
                        var d = (GameObject)PrefabUtility.InstantiatePrefab(dummyPrefab, scene);
                        d.transform.SetPositionAndRotation(position + forward * 6f - right * 1.5f, facePlayer);
                    }
                    if (shieldDummyPrefab != null)
                    {
                        var d = (GameObject)PrefabUtility.InstantiatePrefab(shieldDummyPrefab, scene);
                        d.transform.SetPositionAndRotation(position + forward * 6f + right * 1.5f, facePlayer);
                    }
                    report.AppendLine("Level_01: added two training dummies 6 m in front of the player (move them if they're inside a crate).");
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            if (!string.IsNullOrEmpty(originalScene)) EditorSceneManager.OpenScene(originalScene, OpenSceneMode.Single);
            EditorUtility.DisplayDialog("Replace demo player", report.Length > 0 ? report.ToString() : "No Level_ scenes found in Assets/Scenes.", "OK");
        }

        // ------------------------------------------------------------------ helpers

        private static GameObject FindInScene(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == objectName) return root;
                Transform found = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == objectName);
                if (found != null) return found.gameObject;
            }
            return null;
        }

        private static Transform Child(Transform parent, string name, Vector3 localPosition)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = localPosition;
            return t;
        }

        private static void Box(Transform parent, string name, Vector3 localPosition, Vector3 size, Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            Object.DestroyImmediate(box.GetComponent<Collider>());
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localScale = size;
            var renderer = box.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        private static void Set(Object target, string property, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(property).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Set(Object target, string property, float value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(property).floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Set(Object target, string property, string value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(property).stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Material LitMaterial(string name, Color colour, float metallic = 0f, float smoothness = 0.5f, Color? emission = null)
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

        private static Material UnlitParticleMaterial(string name, Color colour)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Universal Render Pipeline/Unlit");
            var m = new Material(shader);
            m.SetColor("_BaseColor", colour);
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
