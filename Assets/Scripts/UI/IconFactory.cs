using System;
using System.Collections.Generic;
using UnityEngine;

namespace TheLostDungeon.UI
{
    /// <summary>
    /// Draws the UI icons (lock, star, check, play, heart, shield, dash, key...) in code, so the
    /// UI works with no imported art. Each icon is a SIGNED DISTANCE FUNCTION: for every pixel we
    /// compute how far it is from the shape's edge (negative = inside) and turn that distance into
    /// an anti-aliased alpha value. Icons are white, so an Image's colour tints them.
    ///
    /// Sprites are created once and cached. "Bake UI Prefab" saves them as PNG files in
    /// Assets/UI/Icons and lists them in Resources/UIIconSet: from then on those files are used,
    /// so you can repaint or replace any icon there and the whole UI picks it up.
    /// </summary>
    public static class IconFactory
    {
        private const int IconSize = 96;
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        private static UIIconSet iconSet;
        private static bool iconSetLoaded;

        /// <summary>The editor bake tool sets this to get the generated (not overridden) icons.</summary>
        public static bool IgnoreIconSet { get; set; }

        /// <summary>Every icon key with its generator, for the bake tool.</summary>
        public static IEnumerable<KeyValuePair<string, Func<Sprite>>> All()
        {
            yield return new KeyValuePair<string, Func<Sprite>>("white", () => White);
            yield return new KeyValuePair<string, Func<Sprite>>("panel", () => Panel);
            yield return new KeyValuePair<string, Func<Sprite>>("circle", () => Circle);
            yield return new KeyValuePair<string, Func<Sprite>>("ring", () => Ring);
            yield return new KeyValuePair<string, Func<Sprite>>("arc", () => Arc);
            yield return new KeyValuePair<string, Func<Sprite>>("lock", () => Lock);
            yield return new KeyValuePair<string, Func<Sprite>>("unlock", () => Unlock);
            yield return new KeyValuePair<string, Func<Sprite>>("star", () => Star);
            yield return new KeyValuePair<string, Func<Sprite>>("starOutline", () => StarOutline);
            yield return new KeyValuePair<string, Func<Sprite>>("check", () => Check);
            yield return new KeyValuePair<string, Func<Sprite>>("play", () => Play);
            yield return new KeyValuePair<string, Func<Sprite>>("heart", () => Heart);
            yield return new KeyValuePair<string, Func<Sprite>>("shield", () => Shield);
            yield return new KeyValuePair<string, Func<Sprite>>("dash", () => Dash);
            yield return new KeyValuePair<string, Func<Sprite>>("key", () => Key);
            yield return new KeyValuePair<string, Func<Sprite>>("vignette", () => Vignette);
        }

        /// <summary>Forgets cached sprites and re-reads Resources/UIIconSet (used after baking).</summary>
        public static void ResetCache()
        {
            Cache.Clear();
            iconSet = null;
            iconSetLoaded = false;
        }

        /// <summary>Panel sprites are 9-sliced with this border (pixels).</summary>
        public const int PanelBorder = 12;

        public static Sprite White => Get("white", () => Solid(4));
        public static Sprite Panel => Get("panel", () => RoundedPanel(32, 10f, PanelBorder));
        public static Sprite Circle => Icon("circle", p => Length(p) - 0.9f);
        public static Sprite Ring => Icon("ring", p => Mathf.Abs(Length(p) - 0.8f) - 0.08f);
        public static Sprite Arc => Icon("arc", p => p.y > 0f || p.x > 0f ? Mathf.Abs(Length(p) - 0.75f) - 0.11f : 1f);
        public static Sprite Lock => Icon("lock", LockShape);
        public static Sprite Unlock => Icon("unlock", UnlockShape);
        public static Sprite Star => Icon("star", p => Star5(p, 0.92f, 0.45f));
        public static Sprite StarOutline => Icon("starOutline", p => Mathf.Abs(Star5(p, 0.85f, 0.45f)) - 0.06f);
        public static Sprite Check => Icon("check", p => Mathf.Min(Segment(p, new Vector2(-0.6f, 0.0f), new Vector2(-0.15f, -0.45f)),
                                                                   Segment(p, new Vector2(-0.15f, -0.45f), new Vector2(0.65f, 0.5f))) - 0.13f);
        public static Sprite Play => Icon("play", p => Triangle(p, new Vector2(-0.45f, 0.65f), new Vector2(-0.45f, -0.65f), new Vector2(0.7f, 0f)) - 0.05f);
        public static Sprite Heart => Icon("heart", HeartShape);
        public static Sprite Shield => Icon("shield", ShieldShape);
        public static Sprite Dash => Icon("dash", DashShape);
        public static Sprite Key => Icon("key", KeyShape);
        public static Sprite Vignette => Get("vignette", RadialVignette);

        /// <summary>Picks a generated icon for an ability or item id when no sprite is assigned.</summary>
        public static Sprite ForId(string id)
        {
            string lower = (id ?? string.Empty).ToLowerInvariant();
            if (lower.Contains("dash")) return Dash;
            if (lower.Contains("shield")) return Shield;
            if (lower.Contains("key")) return Key;
            if (lower.Contains("heal") || lower.Contains("med")) return Heart;
            return Circle;
        }

        // ---------------------------------------------------------------- shapes

        private static float LockShape(Vector2 p) => Padlock(p, 0.2f, -0.1f);

        private static float UnlockShape(Vector2 p) => Padlock(p, 0.45f, 0.28f);

        /// <summary>Body + shackle. Raising the shackle and shortening its right leg "opens" it.</summary>
        private static float Padlock(Vector2 p, float shackleCentreY, float rightLegEndY)
        {
            float body = RoundedBox(p - new Vector2(0f, -0.35f), new Vector2(0.6f, 0.42f), 0.1f);
            float shackle;
            if (p.y >= shackleCentreY)
            {
                shackle = Mathf.Abs(Length(p - new Vector2(0f, shackleCentreY)) - 0.36f) - 0.1f;
            }
            else
            {
                float left = Segment(p, new Vector2(-0.36f, shackleCentreY), new Vector2(-0.36f, -0.1f));
                float right = Segment(p, new Vector2(0.36f, shackleCentreY), new Vector2(0.36f, rightLegEndY));
                shackle = Mathf.Min(left, right) - 0.1f;
            }
            float keyhole = Length(p - new Vector2(0f, -0.3f)) - 0.1f;
            return Mathf.Max(Mathf.Min(body, shackle), -keyhole);
        }

        private static float HeartShape(Vector2 p)
        {
            // Inigo Quilez's heart SDF, scaled to fit the icon.
            p = new Vector2(Mathf.Abs(p.x), p.y + 0.75f) / 1.35f;
            if (p.y + p.x > 1f)
                return (Length(p - new Vector2(0.25f, 0.75f)) - Mathf.Sqrt(2f) / 4f) * 1.35f;
            float a = Length(p - new Vector2(0f, 1f));
            float m = 0.5f * Mathf.Max(p.x + p.y, 0f);
            float b = Length(p - new Vector2(m, m));
            return Mathf.Sqrt(Mathf.Min(a * a, b * b)) * Mathf.Sign(p.x - p.y) * 1.35f;
        }

        private static float ShieldShape(Vector2 p)
        {
            // A lens (intersection of two circles) cut flat at the top.
            float lens = Mathf.Max(Length(p - new Vector2(0.45f, 0.25f)) - 1f, Length(p - new Vector2(-0.45f, 0.25f)) - 1f);
            return Mathf.Max(lens, p.y - 0.62f);
        }

        private static float DashShape(Vector2 p)
        {
            float a = Mathf.Min(Segment(p, new Vector2(-0.65f, 0.5f), new Vector2(-0.15f, 0f)), Segment(p, new Vector2(-0.15f, 0f), new Vector2(-0.65f, -0.5f)));
            float b = Mathf.Min(Segment(p, new Vector2(0.05f, 0.5f), new Vector2(0.55f, 0f)), Segment(p, new Vector2(0.55f, 0f), new Vector2(0.05f, -0.5f)));
            return Mathf.Min(a, b) - 0.12f;
        }

        private static float KeyShape(Vector2 p)
        {
            float ring = Mathf.Abs(Length(p - new Vector2(-0.45f, 0f)) - 0.3f) - 0.1f;
            float shaft = RoundedBox(p - new Vector2(0.25f, 0f), new Vector2(0.45f, 0.08f), 0.02f);
            float tooth1 = RoundedBox(p - new Vector2(0.5f, -0.18f), new Vector2(0.07f, 0.12f), 0.02f);
            float tooth2 = RoundedBox(p - new Vector2(0.25f, -0.15f), new Vector2(0.06f, 0.09f), 0.02f);
            return Mathf.Min(Mathf.Min(ring, shaft), Mathf.Min(tooth1, tooth2));
        }

        // ---------------------------------------------------------------- SDF helpers

        private static float Length(Vector2 v) => Mathf.Sqrt(v.x * v.x + v.y * v.y);

        private static float RoundedBox(Vector2 p, Vector2 half, float radius)
        {
            float qx = Mathf.Abs(p.x) - half.x + radius;
            float qy = Mathf.Abs(p.y) - half.y + radius;
            float outside = Length(new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)));
            return outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
        }

        private static float Segment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 pa = p - a, ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return Length(pa - ba * h);
        }

        private static float Triangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            // Distance to the edges, negative inside (sign from the winding test).
            float d = Mathf.Min(Segment(p, a, b), Mathf.Min(Segment(p, b, c), Segment(p, c, a)));
            float s1 = Cross(b - a, p - a), s2 = Cross(c - b, p - b), s3 = Cross(a - c, p - c);
            bool inside = (s1 >= 0 && s2 >= 0 && s3 >= 0) || (s1 <= 0 && s2 <= 0 && s3 <= 0);
            return inside ? -d : d;
        }

        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        private static float Star5(Vector2 p, float r, float rf)
        {
            // Inigo Quilez's 5-point star SDF.
            Vector2 k1 = new Vector2(0.809016994f, -0.587785252f);
            Vector2 k2 = new Vector2(-k1.x, k1.y);
            p.x = Mathf.Abs(p.x);
            p -= 2f * Mathf.Max(Vector2.Dot(k1, p), 0f) * k1;
            p -= 2f * Mathf.Max(Vector2.Dot(k2, p), 0f) * k2;
            p.x = Mathf.Abs(p.x);
            p.y -= r;
            Vector2 ba = rf * new Vector2(-k1.y, k1.x) - new Vector2(0f, 1f);
            float h = Mathf.Clamp(Vector2.Dot(p, ba) / Vector2.Dot(ba, ba), 0f, r);
            return Length(p - ba * h) * Mathf.Sign(p.y * ba.x - p.x * ba.y);
        }

        // ---------------------------------------------------------------- texture builders

        private static Sprite Get(string key, Func<Sprite> create)
        {
            if (!IgnoreIconSet)
            {
                if (!iconSetLoaded)
                {
                    iconSet = Resources.Load<UIIconSet>("UIIconSet");
                    iconSetLoaded = true;
                }
                Sprite fromAsset = iconSet != null ? iconSet.Find(key) : null;
                if (fromAsset != null) return fromAsset;
            }

            if (Cache.TryGetValue(key, out Sprite sprite) && sprite != null) return sprite;
            sprite = create();
            sprite.name = "Icon_" + key;
            Cache[key] = sprite;
            return sprite;
        }

        private static Sprite Icon(string key, Func<Vector2, float> shape) => Get(key, () => Rasterise(shape, IconSize));

        private static Sprite Rasterise(Func<Vector2, float> shape, int size)
        {
            var tex = NewTexture(size);
            var pixels = new Color32[size * size];
            float pixel = 2f / size;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Map the pixel centre to -1..1, y up.
                    Vector2 p = new Vector2((x + 0.5f) * pixel - 1f, (y + 0.5f) * pixel - 1f);
                    float alpha = Mathf.Clamp01(0.5f - shape(p) / pixel);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, false);   // stays readable so the bake tool can save it as PNG
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite Solid(int size)
        {
            var tex = NewTexture(size);
            var pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(pixels);
            tex.Apply(false, false);   // stays readable so the bake tool can save it as PNG
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>A rounded rectangle with 9-slice borders, used for every panel and button.</summary>
        private static Sprite RoundedPanel(int size, float radius, int border)
        {
            var tex = NewTexture(size);
            var pixels = new Color32[size * size];
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f - half, y + 0.5f - half);
                    float d = RoundedBox(p, new Vector2(half, half), radius);
                    float alpha = Mathf.Clamp01(0.5f - d);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, false);   // stays readable so the bake tool can save it as PNG
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                                 SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        private static Sprite RadialVignette()
        {
            const int size = 128;
            var tex = NewTexture(size);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1.3f, d));
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, false);   // stays readable so the bake tool can save it as PNG
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Texture2D NewTexture(int size) =>
            new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };
    }
}
