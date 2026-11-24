using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TheLostDungeon.UI
{
    /// <summary>The UI's default colours. Used when an element is first created (or baked).</summary>
    public static class UITheme
    {
        public static readonly Color Background = new Color(0.03f, 0.05f, 0.08f, 0.96f);
        public static readonly Color Dim = new Color(0f, 0f, 0f, 0.65f);
        public static readonly Color PanelColour = new Color(0.07f, 0.1f, 0.14f, 0.92f);
        public static readonly Color Button = new Color(0.1f, 0.15f, 0.21f, 1f);
        public static readonly Color ButtonHover = new Color(0.13f, 0.36f, 0.46f, 1f);
        public static readonly Color Accent = new Color(0.25f, 0.85f, 1f, 1f);
        public static readonly Color Text = new Color(0.9f, 0.95f, 1f, 1f);
        public static readonly Color TextMuted = new Color(0.55f, 0.65f, 0.75f, 1f);
        public static readonly Color Health = new Color(0.3f, 0.95f, 0.45f, 1f);
        public static readonly Color Danger = new Color(1f, 0.3f, 0.25f, 1f);
        public static readonly Color Warning = new Color(1f, 0.65f, 0.15f, 1f);
        public static readonly Color Gold = new Color(1f, 0.82f, 0.25f, 1f);
        public static readonly Color Locked = new Color(0.4f, 0.45f, 0.5f, 1f);
    }

    /// <summary>
    /// Builds uGUI elements by NAME with "get or create" semantics:
    ///
    ///   - If a child with that name already exists (because the UI was baked into the editable
    ///     GameUI prefab and you edited it), it is REUSED and its look is left exactly as you
    ///     set it in the editor: position, size, colour, font, sprite, text.
    ///   - If it doesn't exist, it is created with the default style below.
    ///
    /// So the same screen code works with no prefab (everything generated) and with your edited
    /// prefab (only the behaviour is attached: button clicks, events, live values).
    /// </summary>
    public static class UIBuilder
    {
        private static readonly HashSet<Transform> CreatedThisSession = new HashSet<Transform>();
        private static Font font;

        public static Font Font
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        /// <summary>True if this element was just created (not loaded from the edited prefab).</summary>
        public static bool IsNew(Component component) =>
            component != null && CreatedThisSession.Contains(component.transform);

        public static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T existing = go.GetComponent<T>();
            return existing != null ? existing : go.AddComponent<T>();
        }

        // ---------------------------------------------------------------- layout

        public static RectTransform Rect(string name, Transform parent)
        {
            Transform existing = parent.Find(name);
            if (existing is RectTransform found) return found;

            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;   // UI
            go.transform.SetParent(parent, false);
            CreatedThisSession.Add(go.transform);
            return (RectTransform)go.transform;
        }

        public static RectTransform Stretch(RectTransform rt, float inset = 0f)
        {
            if (!IsNew(rt)) return rt;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            return rt;
        }

        /// <summary>Anchors a rect to one point of its parent (0..1) with a fixed size.</summary>
        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
        {
            if (!IsNew(rt)) return rt;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        public static VerticalLayoutGroup Vertical(Component target, float spacing, TextAnchor align, int padding = 0)
        {
            var existing = target.GetComponent<VerticalLayoutGroup>();
            if (existing != null) return existing;
            var layout = target.gameObject.AddComponent<VerticalLayoutGroup>();
            Configure(layout, spacing, align, padding);
            return layout;
        }

        public static HorizontalLayoutGroup Horizontal(Component target, float spacing, TextAnchor align, int padding = 0)
        {
            var existing = target.GetComponent<HorizontalLayoutGroup>();
            if (existing != null) return existing;
            var layout = target.gameObject.AddComponent<HorizontalLayoutGroup>();
            Configure(layout, spacing, align, padding);
            return layout;
        }

        private static void Configure(HorizontalOrVerticalLayoutGroup layout, float spacing, TextAnchor align, int padding)
        {
            layout.spacing = spacing;
            layout.childAlignment = align;
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        public static T Size<T>(T component, float width, float height) where T : Component
        {
            if (!IsNew(component)) return component;
            var element = GetOrAdd<LayoutElement>(component.gameObject);
            if (width >= 0f) { element.preferredWidth = width; element.minWidth = width; }
            if (height >= 0f) { element.preferredHeight = height; element.minHeight = height; }
            return component;
        }

        // ---------------------------------------------------------------- graphics

        public static Image Image(Transform parent, string name, Color colour, Sprite sprite = null)
        {
            RectTransform rt = Rect(name, parent);
            bool isNew = IsNew(rt);
            var image = GetOrAdd<Image>(rt.gameObject);
            if (isNew)
            {
                image.sprite = sprite != null ? sprite : IconFactory.White;
                image.color = colour;
                image.raycastTarget = false;
            }
            return image;
        }

        /// <summary>A rounded, 9-sliced panel.</summary>
        public static Image Panel(Transform parent, string name, Color colour)
        {
            Image image = Image(parent, name, colour, IconFactory.Panel);
            if (IsNew(image)) image.type = UnityEngine.UI.Image.Type.Sliced;
            return image;
        }

        public static Image Icon(Transform parent, string name, Sprite sprite, Color colour, float size)
        {
            Image image = Image(parent, name, colour, sprite);
            if (IsNew(image))
            {
                image.preserveAspect = true;
                image.rectTransform.sizeDelta = new Vector2(size, size);
            }
            return image;
        }

        public static Text Label(Transform parent, string name, string text, int size, Color colour,
                                 TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            RectTransform rt = Rect(name, parent);
            bool isNew = IsNew(rt);
            var label = GetOrAdd<Text>(rt.gameObject);
            if (!isNew) return label;

            label.font = Font;
            label.text = text;
            label.fontSize = size;
            label.color = colour;
            label.alignment = align;
            label.fontStyle = style;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            var shadow = GetOrAdd<Shadow>(rt.gameObject);
            shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
            return label;
        }

        /// <summary>A horizontal progress bar: returns the background; the fill comes back via out.</summary>
        public static Image Bar(Transform parent, string name, Color background, Color fillColour, out Image fill)
        {
            Image bg = Panel(parent, name, background);
            fill = Image(bg.transform, "Fill", fillColour);
            fill.type = UnityEngine.UI.Image.Type.Filled;          // functional, always applied
            fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            Stretch(fill.rectTransform, 3f);
            return bg;
        }

        // ---------------------------------------------------------------- controls

        /// <summary>A styled button that plays the UI click (or back) sound.</summary>
        public static Button Button(Transform parent, string label, UnityAction onClick, float width = 420f,
                                    float height = 64f, bool isBack = false, int fontSize = 26)
        {
            Image background = Panel(parent, "Button_" + label, Color.white);
            bool isNew = IsNew(background);
            var button = GetOrAdd<Button>(background.gameObject);

            if (isNew)
            {
                background.raycastTarget = true;
                Size(background, width, height);
                button.targetGraphic = background;
                ColorBlock colours = button.colors;
                colours.normalColor = UITheme.Button;
                colours.highlightedColor = UITheme.ButtonHover;
                colours.selectedColor = UITheme.ButtonHover;
                colours.pressedColor = UITheme.Accent;
                colours.disabledColor = new Color(0.08f, 0.1f, 0.13f, 0.6f);
                colours.fadeDuration = 0.08f;
                button.colors = colours;

                Text text = Label(background.transform, "Label", label, fontSize, UITheme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
                Stretch(text.rectTransform, 6f);
            }

            // Behaviour is always attached at runtime (never saved in the prefab).
            button.onClick.AddListener(() =>
            {
                if (isBack) UISounds.Back(); else UISounds.Click();
            });
            if (onClick != null) button.onClick.AddListener(onClick);
            return button;
        }

        public static void SetButtonText(Button button, string text)
        {
            Text label = button.GetComponentInChildren<Text>(true);
            if (label != null) label.text = text;
        }

        public static void SetInteractable(Button button, bool interactable)
        {
            button.interactable = interactable;
            Text label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                Color c = label.color;
                c.a = interactable ? 1f : 0.35f;
                label.color = c;
            }
        }

        public static Slider Slider(Transform parent, string name, float min, float max, float value,
                                    UnityAction<float> onChanged, float width = 420f)
        {
            RectTransform root = Rect(name, parent);
            Slider slider;

            if (IsNew(root))
            {
                Size(root, width, 36f);

                Image background = Panel(root, "Background", new Color(0.12f, 0.16f, 0.22f, 1f));
                background.rectTransform.anchorMin = Vector2.zero;
                background.rectTransform.anchorMax = Vector2.one;
                background.rectTransform.offsetMin = new Vector2(0f, 12f);
                background.rectTransform.offsetMax = new Vector2(0f, -12f);

                RectTransform fillArea = Rect("Fill Area", root);
                fillArea.anchorMin = Vector2.zero;
                fillArea.anchorMax = Vector2.one;
                fillArea.offsetMin = new Vector2(4f, 12f);
                fillArea.offsetMax = new Vector2(-4f, -12f);
                Image fill = Panel(fillArea, "Fill", UITheme.Accent);
                fill.rectTransform.sizeDelta = Vector2.zero;

                RectTransform handleArea = Rect("Handle Area", root);
                handleArea.anchorMin = Vector2.zero;
                handleArea.anchorMax = Vector2.one;
                handleArea.offsetMin = new Vector2(14f, 0f);
                handleArea.offsetMax = new Vector2(-14f, 0f);
                Image handle = Image(handleArea, "Handle", UITheme.Text, IconFactory.Circle);
                handle.rectTransform.sizeDelta = new Vector2(30f, 0f);
                handle.raycastTarget = true;

                slider = root.gameObject.AddComponent<Slider>();
                slider.fillRect = fill.rectTransform;
                slider.handleRect = handle.rectTransform;
                slider.targetGraphic = handle;
                slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            }
            else
            {
                slider = GetOrAdd<Slider>(root.gameObject);
            }

            slider.minValue = min;
            slider.maxValue = max;
            slider.SetValueWithoutNotify(value);
            if (onChanged != null) slider.onValueChanged.AddListener(onChanged);
            return slider;
        }

        /// <summary>A single-line text field (player name). onSubmit runs when editing ends.</summary>
        public static InputField InputField(Transform parent, string name, string value, int characterLimit,
                                            UnityAction<string> onSubmit, float width = 360f)
        {
            Image background = Panel(parent, name, new Color(0.12f, 0.16f, 0.22f, 1f));
            bool isNew = IsNew(background);
            var field = GetOrAdd<InputField>(background.gameObject);

            if (isNew)
            {
                background.raycastTarget = true;
                Size(background, width, 52f);

                Text placeholder = Label(background.transform, "Placeholder", "Enter a name...", 24, UITheme.TextMuted, TextAnchor.MiddleLeft, FontStyle.Italic);
                Stretch(placeholder.rectTransform, 12f);
                Text text = Label(background.transform, "Text", "", 24, UITheme.Text, TextAnchor.MiddleLeft);
                Stretch(text.rectTransform, 12f);
                text.supportRichText = false;

                field.targetGraphic = background;
                field.textComponent = text;
                field.placeholder = placeholder;
            }

            field.characterLimit = characterLimit;
            field.SetTextWithoutNotify(value);
            if (onSubmit != null) field.onEndEdit.AddListener(onSubmit);
            return field;
        }

        /// <summary>Fills the parent with a translucent colour that also blocks clicks to the game.</summary>
        public static Image Backdrop(Transform parent, Color colour)
        {
            Image image = Image(parent, "Backdrop", colour);
            Stretch(image.rectTransform);
            image.raycastTarget = true;
            return image;
        }

        public static string FormatTime(float seconds)
        {
            if (seconds < 0f) seconds = 0f;
            int total = Mathf.FloorToInt(seconds);
            return $"{total / 60}:{total % 60:00}";
        }
    }
}
