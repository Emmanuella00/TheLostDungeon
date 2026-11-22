using System.IO;
using System.Linq;
using TheLostDungeon.Pooling;
using TheLostDungeon.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TheLostDungeon.EditorTools
{
    /// <summary>
    /// Tools > The Lost Dungeon > Create Weapon Effects (Fire, Sparks, Smoke)
    ///
    /// Builds two pooled particle effects with proper materials, so particles render as soft glowing
    /// dots and puffs instead of solid squares:
    ///   FX_MuzzleFlash  = fire flash + sparks + smoke puff
    ///   FX_ImpactSpark  = spark burst + dust puff
    /// It generates the round textures, creates transparent (additive / alpha) particle materials,
    /// assigns both effects to the BlasterWeapon on the Player prefab, and clears any scene
    /// overrides of those fields in the Level scenes so the prefab values are used everywhere.
    ///
    /// Tweak the results freely afterwards in the Inspector (colours, sizes, lifetimes).
    /// </summary>
    public static class WeaponEffectsSetup
    {
        private const string Folder = "Assets/Prefabs/Effects";
        private const string PlayerPath = "Assets/Prefabs/Player.prefab";
        private const string MuzzlePath = "Assets/Prefabs/FX_MuzzleFlash.prefab";
        private const string ImpactPath = "Assets/Prefabs/FX_ImpactSpark.prefab";

        private static readonly Color Fire = new Color(1f, 0.62f, 0.2f);
        private static readonly Color FireCore = new Color(1f, 0.9f, 0.55f);
        private static readonly Color Smoke = new Color(0.55f, 0.55f, 0.55f, 0.35f);

        [MenuItem("Tools/The Lost Dungeon/Create Weapon Effects (Fire, Sparks, Smoke)")]
        public static void CreateEffects()
        {
            EnsureFolder(Folder);

            Texture2D glowTex = SoftDotTexture("T_SoftGlow", size: 64, hardness: 2.2f, noise: 0f);
            Texture2D smokeTex = SoftDotTexture("T_SmokePuff", size: 64, hardness: 1.2f, noise: 0.35f);

            Material additive = ParticleMaterial("M_FX_Additive", glowTex, additive: true);
            Material smoke = ParticleMaterial("M_FX_Smoke", smokeTex, additive: false);

            GameObject muzzle = BuildMuzzleFlash(additive, smoke);
            GameObject impact = BuildImpact(additive, smoke);

            bool assigned = AssignToPlayer(muzzle, impact);
            int scenesFixed = ClearSceneOverrides();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Weapon effects",
                "Created:\n• " + MuzzlePath + " (fire flash, sparks, smoke)\n• " + ImpactPath + " (sparks, dust)\n" +
                "• Materials and textures in " + Folder + "\n\n" +
                (assigned ? "Assigned to the BlasterWeapon on the Player prefab.\n" : "Couldn't find the Player prefab; assign them by hand.\n") +
                $"Cleared old effect overrides in {scenesFixed} level scene(s).",
                "OK");
        }

        // ------------------------------------------------------------------ effects

        private static GameObject BuildMuzzleFlash(Material additive, Material smoke)
        {
            // Root particle system = the fire flash. PooledParticleEffect plays it and its children.
            var root = new GameObject("FX_MuzzleFlash");
            try
            {
                ParticleSystem flash = AddParticles(root, additive, lifetime: 0.06f, speed: (0f, 0.2f), size: (0.07f, 0.11f),
                                                    burst: 3, colourA: FireCore, colourB: Fire, coneAngle: 5f,
                                                    worldSpace: false, shrink: true, fade: true);

                var sparks = new GameObject("Sparks");
                sparks.transform.SetParent(root.transform, false);
                ParticleSystem sparkPs = AddParticles(sparks, additive, lifetime: 0.12f, speed: (2f, 5f), size: (0.008f, 0.018f),
                                                      burst: 6, colourA: FireCore, colourB: Fire, coneAngle: 18f,
                                                      worldSpace: true, shrink: true, fade: true);
                StretchSparks(sparkPs);

                var smokeGo = new GameObject("Smoke");
                smokeGo.transform.SetParent(root.transform, false);
                ParticleSystem smokePs = AddParticles(smokeGo, smoke, lifetime: 0.9f, speed: (0.15f, 0.45f), size: (0.04f, 0.07f),
                                                      burst: 4, colourA: Smoke, colourB: Smoke, coneAngle: 15f,
                                                      worldSpace: true, shrink: false, fade: true);
                Grow(smokePs, 4f);
                ParticleSystem.MainModule smokeMain = smokePs.main;
                smokeMain.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                smokeMain.gravityModifier = -0.03f;   // drifts up slowly

                FinishPooled(root, autoReturnSeconds: 1.1f);
                return PrefabUtility.SaveAsPrefabAsset(root, MuzzlePath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject BuildImpact(Material additive, Material smoke)
        {
            var root = new GameObject("FX_ImpactSpark");
            try
            {
                ParticleSystem sparks = AddParticles(root, additive, lifetime: 0.25f, speed: (2f, 5.5f), size: (0.01f, 0.025f),
                                                     burst: 12, colourA: FireCore, colourB: Fire, coneAngle: 40f,
                                                     worldSpace: true, shrink: true, fade: true);
                ParticleSystem.MainModule main = sparks.main;
                main.gravityModifier = 0.8f;
                StretchSparks(sparks);

                var dust = new GameObject("Dust");
                dust.transform.SetParent(root.transform, false);
                ParticleSystem dustPs = AddParticles(dust, smoke, lifetime: 0.6f, speed: (0.2f, 0.6f), size: (0.06f, 0.1f),
                                                     burst: 3, colourA: Smoke, colourB: Smoke, coneAngle: 30f,
                                                     worldSpace: true, shrink: false, fade: true);
                Grow(dustPs, 2.5f);

                FinishPooled(root, autoReturnSeconds: 0.8f);
                return PrefabUtility.SaveAsPrefabAsset(root, ImpactPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        internal static ParticleSystem AddParticles(GameObject go, Material material, float lifetime, (float min, float max) speed,
                                                   (float min, float max) size, int burst, Color colourA, Color colourB,
                                                   float coneAngle, bool worldSpace, bool shrink, bool fade)
        {
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.duration = Mathf.Max(0.05f, lifetime);
            main.loop = false;
            main.playOnAwake = false;    // the pool plays it
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.7f, lifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed.min, speed.max);
            main.startSize = new ParticleSystem.MinMaxCurve(size.min, size.max);
            main.startColor = new ParticleSystem.MinMaxGradient(colourA, colourB);
            main.simulationSpace = worldSpace ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
            main.maxParticles = Mathf.Max(burst * 2, 8);

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burst) });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = coneAngle;
            shape.radius = 0.005f;

            if (shrink) Grow(ps, 0f);

            if (fade)
            {
                ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
                col.enabled = true;
                var gradient = new Gradient();
                gradient.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.4f), new GradientAlphaKey(0f, 1f) });
                col.color = gradient;
            }

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return ps;
        }

        /// <summary>Size over lifetime: endScale 0 = shrinks away, 4 = grows to 4x (smoke).</summary>
        internal static void Grow(ParticleSystem ps, float endScale)
        {
            ParticleSystem.SizeOverLifetimeModule sol = ps.sizeOverLifetime;
            sol.enabled = true;
            // Curve values must stay in 0..1, so growth uses multiplier = endScale with a 1/endScale..1 curve.
            if (endScale >= 1f)
                sol.size = new ParticleSystem.MinMaxCurve(endScale, AnimationCurve.Linear(0f, 1f / endScale, 1f, 1f));
            else
                sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, endScale));
        }

        /// <summary>Draws sparks as short streaks along their direction of travel.</summary>
        private static void StretchSparks(ParticleSystem ps)
        {
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.04f;
            renderer.lengthScale = 2f;
        }

        internal static void FinishPooled(GameObject root, float autoReturnSeconds)
        {
            root.AddComponent<PooledParticleEffect>();
            PooledObject pooled = root.AddComponent<PooledObject>();
            var so = new SerializedObject(pooled);
            so.FindProperty("autoReturnSeconds").floatValue = autoReturnSeconds;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------------ wiring

        private static bool AssignToPlayer(GameObject muzzle, GameObject impact)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath) == null) return false;

            GameObject contents = PrefabUtility.LoadPrefabContents(PlayerPath);
            try
            {
                BlasterWeapon blaster = contents.GetComponentInChildren<BlasterWeapon>(true);
                if (blaster == null) return false;

                var so = new SerializedObject(blaster);
                so.FindProperty("muzzleFlashPrefab").objectReferenceValue = muzzle;
                so.FindProperty("impactEffectPrefab").objectReferenceValue = impact;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, PlayerPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        /// <summary>
        /// A scene can override a prefab's field (Level_01 still pointed at the deleted effect).
        /// Reverting those overrides makes every level use the Player prefab's effects.
        /// </summary>
        private static int ClearSceneOverrides()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return 0;

            string original = SceneManager.GetActiveScene().path;
            int changedScenes = 0;

            string[] paths = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => Path.GetFileNameWithoutExtension(p).StartsWith("Level_"))
                .ToArray();

            foreach (string path in paths)
            {
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                bool changed = false;

                foreach (BlasterWeapon blaster in Object.FindObjectsByType<BlasterWeapon>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    var so = new SerializedObject(blaster);
                    foreach (string field in new[] { "muzzleFlashPrefab", "impactEffectPrefab" })
                    {
                        SerializedProperty prop = so.FindProperty(field);
                        if (prop != null && prop.prefabOverride)
                        {
                            PrefabUtility.RevertPropertyOverride(prop, InteractionMode.AutomatedAction);
                            changed = true;
                        }
                    }
                }

                if (changed)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    changedScenes++;
                }
            }

            if (!string.IsNullOrEmpty(original)) EditorSceneManager.OpenScene(original, OpenSceneMode.Single);
            return changedScenes;
        }

        // ------------------------------------------------------------------ textures & materials

        /// <summary>White texture with a round, soft alpha falloff (plus optional noise for smoke).</summary>
        private static Texture2D SoftDotTexture(string name, int size, float hardness, float noise)
        {
            string path = $"{Folder}/{name}.png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = (size - 1) * 0.5f;
            var random = new System.Random(1234);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Pow(Mathf.Clamp01(1f - d), hardness);
                    if (noise > 0f) alpha *= 1f - noise * (float)random.NextDouble();
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>URP Particles/Unlit, transparent. Additive glows (fire, sparks); alpha blends (smoke).</summary>
        private static Material ParticleMaterial(string name, Texture texture, bool additive)
        {
            string path = $"{Folder}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            var m = new Material(shader);
            m.SetTexture("_BaseMap", texture);
            m.SetColor("_BaseColor", Color.white);

            m.SetFloat("_Surface", 1f);                       // Transparent
            m.SetFloat("_Blend", additive ? 2f : 0f);         // 2 = Additive, 0 = Alpha
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;

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
