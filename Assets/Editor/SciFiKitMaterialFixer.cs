// Editor-only tool. Lives in an "Editor" folder so it is never included in builds.
//
// Problem it fixes:
//   The Sci-Fi Warehouse Kit materials ended up using URP's 2D sprite shader
//   ("Universal Render Pipeline/2D/Mesh2D-Lit-Default"). That shader is alpha-blended
//   and ignores 3D lights, so in a 3D scene every prop looks flat, washed out and
//   see-through.
//
// What it does:
//   Tools > The Lost Dungeon > Fix SciFi Kit Materials
//     Re-assigns every material in the kit to "Universal Render Pipeline/Lit" and copies
//     over the original Standard-shader values (albedo, normal map, metallic, smoothness,
//     emission, and Opaque / Cutout / Fade mode).
//
//   Tools > The Lost Dungeon > Limit Shadow-Casting Lights In Open Scene
//     Turns shadows off on point/spot lights, keeping only the first few, so URP's
//     shadow atlas stops overflowing. Undo (Ctrl+Z) works for both tools.

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class SciFiKitMaterialFixer
{
    private const string MaterialsFolder = "Assets/SciFi Warehouse Kit/Art/Materials";
    private const string UrpLitShaderName = "Universal Render Pipeline/Lit";
    private const int MaxShadowCastingLocalLights = 6;

    // Unity Standard shader "_Mode" values.
    private const int StandardOpaque = 0;
    private const int StandardCutout = 1;
    // 2 = Fade, 3 = Transparent  -> both become URP Transparent.

    [MenuItem("Tools/The Lost Dungeon/Fix SciFi Kit Materials")]
    public static void FixMaterials()
    {
        Shader litShader = Shader.Find(UrpLitShaderName);
        if (litShader == null)
        {
            EditorUtility.DisplayDialog("Shader not found",
                "Could not find '" + UrpLitShaderName + "'. Is the Universal RP package installed?", "OK");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { MaterialsFolder });
        int converted = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) continue;

            // Leave the skybox (and any other skybox material) alone.
            if (material.shader != null && material.shader.name.StartsWith("Skybox")) continue;

            // Read the ORIGINAL saved values straight from the asset, because the current
            // (wrong) shader does not expose most of them as properties.
            SavedValues saved = SavedValues.Read(material);

            Undo.RecordObject(material, "Fix SciFi Kit Materials");
            material.shader = litShader;
            ApplyToUrpLit(material, saved);
            EditorUtility.SetDirty(material);
            converted++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[SciFiKitMaterialFixer] Converted " + converted + " materials to URP/Lit.");
        EditorUtility.DisplayDialog("Done", "Converted " + converted + " materials to URP Lit.", "OK");
    }

    [MenuItem("Tools/The Lost Dungeon/Limit Shadow-Casting Lights In Open Scene")]
    public static void LimitShadowCastingLights()
    {
        Light[] lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int kept = 0, disabled = 0;

        foreach (Light light in lights)
        {
            bool isLocal = light.type == LightType.Point || light.type == LightType.Spot;
            if (!isLocal || light.shadows == LightShadows.None) continue;

            if (kept < MaxShadowCastingLocalLights && light.gameObject.activeInHierarchy)
            {
                kept++;
                continue;
            }

            Undo.RecordObject(light, "Limit Shadow-Casting Lights");
            light.shadows = LightShadows.None;
            EditorUtility.SetDirty(light);
            disabled++;
        }

        Debug.Log("[SciFiKitMaterialFixer] Shadows kept on " + kept + " lights, turned off on " + disabled + ".");
    }

    private static void ApplyToUrpLit(Material m, SavedValues s)
    {
        // Base colour & texture
        Texture albedo = s.Tex("_MainTex");
        Color color = s.Col("_Color", Color.white);
        m.SetTexture("_BaseMap", albedo);
        m.SetTexture("_MainTex", albedo);
        m.SetColor("_BaseColor", color);
        m.SetColor("_Color", color);

        // Normal map
        Texture normal = s.Tex("_BumpMap");
        m.SetTexture("_BumpMap", normal);
        m.SetFloat("_BumpScale", s.Float("_BumpScale", 1f));
        SetKeyword(m, "_NORMALMAP", normal != null);

        // Metallic workflow
        m.SetFloat("_WorkflowMode", 1f);
        m.SetFloat("_Metallic", s.Float("_Metallic", 0f));
        m.SetFloat("_Smoothness", s.Float("_Glossiness", 0.5f));
        Texture metallicMap = s.Tex("_MetallicGlossMap");
        m.SetTexture("_MetallicGlossMap", metallicMap);
        SetKeyword(m, "_METALLICSPECGLOSSMAP", metallicMap != null);

        // Occlusion
        Texture occlusion = s.Tex("_OcclusionMap");
        m.SetTexture("_OcclusionMap", occlusion);
        m.SetFloat("_OcclusionStrength", s.Float("_OcclusionStrength", 1f));
        SetKeyword(m, "_OCCLUSIONMAP", occlusion != null);

        // Emission (light panels, red signs)
        Color emission = s.Col("_EmissionColor", Color.black);
        Texture emissionMap = s.Tex("_EmissionMap");
        bool emissive = emission.maxColorComponent > 0.001f;
        m.SetColor("_EmissionColor", emission);
        m.SetTexture("_EmissionMap", emissionMap);
        SetKeyword(m, "_EMISSION", emissive);
        m.globalIlluminationFlags = emissive
            ? MaterialGlobalIlluminationFlags.BakedEmissive
            : MaterialGlobalIlluminationFlags.EmissiveIsBlack;

        // Surface type
        int mode = Mathf.RoundToInt(s.Float("_Mode", 0f));
        if (mode == StandardOpaque) SetOpaque(m, alphaClip: false, cutoff: 0.5f);
        else if (mode == StandardCutout) SetOpaque(m, alphaClip: true, cutoff: s.Float("_Cutoff", 0.5f));
        else SetTransparent(m);
    }

    private static void SetOpaque(Material m, bool alphaClip, float cutoff)
    {
        m.SetFloat("_Surface", 0f);
        m.SetFloat("_Blend", 0f);
        m.SetFloat("_SrcBlend", (float)BlendMode.One);
        m.SetFloat("_DstBlend", (float)BlendMode.Zero);
        m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        m.SetFloat("_DstBlendAlpha", (float)BlendMode.Zero);
        m.SetFloat("_ZWrite", 1f);
        m.SetFloat("_AlphaClip", alphaClip ? 1f : 0f);
        m.SetFloat("_Cutoff", cutoff);
        SetKeyword(m, "_SURFACE_TYPE_TRANSPARENT", false);
        SetKeyword(m, "_ALPHATEST_ON", alphaClip);
        m.SetOverrideTag("RenderType", alphaClip ? "TransparentCutout" : "Opaque");
        m.renderQueue = alphaClip ? (int)RenderQueue.AlphaTest : (int)RenderQueue.Geometry;
        m.SetShaderPassEnabled("DepthOnly", true);
        m.SetShaderPassEnabled("ShadowCaster", true);
    }

    private static void SetTransparent(Material m)
    {
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f); // Alpha blending
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.SetFloat("_AlphaClip", 0f);
        SetKeyword(m, "_SURFACE_TYPE_TRANSPARENT", true);
        SetKeyword(m, "_ALPHATEST_ON", false);
        m.SetOverrideTag("RenderType", "Transparent");
        m.renderQueue = (int)RenderQueue.Transparent;
        m.SetShaderPassEnabled("DepthOnly", false);
        m.SetShaderPassEnabled("ShadowCaster", true);
    }

    private static void SetKeyword(Material m, string keyword, bool on)
    {
        if (on) m.EnableKeyword(keyword);
        else m.DisableKeyword(keyword);
    }

    /// <summary>
    /// Reads textures/floats/colours from the material's serialized data, including values
    /// left over from the original Standard shader that the current shader doesn't declare.
    /// </summary>
    private class SavedValues
    {
        private readonly Dictionary<string, Texture> _textures = new Dictionary<string, Texture>();
        private readonly Dictionary<string, float> _floats = new Dictionary<string, float>();
        private readonly Dictionary<string, Color> _colors = new Dictionary<string, Color>();

        public static SavedValues Read(Material material)
        {
            var values = new SavedValues();
            var so = new SerializedObject(material);
            SerializedProperty saved = so.FindProperty("m_SavedProperties");

            SerializedProperty texEnvs = saved.FindPropertyRelative("m_TexEnvs");
            for (int i = 0; i < texEnvs.arraySize; i++)
            {
                SerializedProperty entry = texEnvs.GetArrayElementAtIndex(i);
                string name = entry.FindPropertyRelative("first").stringValue;
                var tex = entry.FindPropertyRelative("second.m_Texture").objectReferenceValue as Texture;
                values._textures[name] = tex;
            }

            SerializedProperty floats = saved.FindPropertyRelative("m_Floats");
            for (int i = 0; i < floats.arraySize; i++)
            {
                SerializedProperty entry = floats.GetArrayElementAtIndex(i);
                values._floats[entry.FindPropertyRelative("first").stringValue] =
                    entry.FindPropertyRelative("second").floatValue;
            }

            SerializedProperty colors = saved.FindPropertyRelative("m_Colors");
            for (int i = 0; i < colors.arraySize; i++)
            {
                SerializedProperty entry = colors.GetArrayElementAtIndex(i);
                values._colors[entry.FindPropertyRelative("first").stringValue] =
                    entry.FindPropertyRelative("second").colorValue;
            }

            return values;
        }

        public Texture Tex(string name) => _textures.TryGetValue(name, out var t) ? t : null;
        public float Float(string name, float fallback) => _floats.TryGetValue(name, out var f) ? f : fallback;
        public Color Col(string name, Color fallback) => _colors.TryGetValue(name, out var c) ? c : fallback;
    }
}
