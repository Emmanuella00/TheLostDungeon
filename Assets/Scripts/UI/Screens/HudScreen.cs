using System.Collections.Generic;
using TheLostDungeon.Core;
using TheLostDungeon.Events;
using TheLostDungeon.Items;
using TheLostDungeon.Levels;
using UnityEngine;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

namespace TheLostDungeon.UI
{
    /// <summary>
    /// In-game HUD. Every element is updated by GameEvents (OBSERVER), never by the player or
    /// enemy scripts directly:
    ///   PlayerHealthChanged  -> health bar, damage vignette, low-health pulse
    ///   AbilityStatusChanged -> ability icons, cooldown sweep, lock overlay
    ///   InventoryChanged     -> item chips (keycards, credits...)
    ///   ObjectiveUpdated     -> objective text + progress bar
    ///   InteractionPrompt    -> "[E] Open door"
    ///   WeaponHeatChanged    -> heat bar under the crosshair
    ///   DamageDealt          -> hit marker
    ///   LevelStarted         -> level name + timer
    /// </summary>
    public class HudScreen : UIScreen
    {
        private class AbilitySlot
        {
            public RectTransform Root;
            public Image Frame;
            public Image Icon;
            public Image Cooldown;
            public Image Lock;
            public Text Key;
            public Text Timer;
            public float CooldownEnd;
            public float CooldownLength;
            public bool Unlocked;
            public bool Active;
            public Color FrameColour;   // colours read from the (possibly edited) prefab
            public Color KeyColour;
        }

        // Base colours read after building, so edits made in the GameUI prefab are respected.
        private Color objectiveColour, healthColour, crosshairColour;

        // Level info
        private Text levelNumber, levelName, levelTimer;
        // Objective
        private Text objectiveText, objectiveCount;
        private Image objectiveFill, objectivePanel;
        private float objectiveFlash;
        // Health
        private Image healthFill;
        private Text healthText;
        private Image heartIcon;
        private Image damageVignette;
        private float damageFlash;
        private float healthFraction = 1f;
        private int lastHealth = -1;
        // Abilities
        private readonly AbilitySlot[] slots = new AbilitySlot[2];
        // Inventory
        private RectTransform inventoryRow;
        private readonly List<GameObject> inventoryChips = new List<GameObject>();
        // Crosshair, hit marker, heat, prompt
        private Image[] crosshair;
        private Image hitMarker;
        private float hitFlash;
        private Image heatBar, heatFill;
        private float heat;
        private bool overheated;
        private Image promptPanel;
        private Text promptText;
        // Mobile
        private RectTransform touchControls;

        protected override void Build()
        {
            BuildDamageVignette();
            BuildLevelInfo();
            BuildObjective();
            BuildHealth();
            BuildAbilities();
            BuildInventory();
            BuildCrosshair();
            BuildPrompt();
            BuildTouchControls();

            objectiveColour = objectivePanel.color;
            healthColour = healthFill.color;
            crosshairColour = crosshair[0].color;
        }

        public override void Subscribe()
        {
            GameEvents.LevelStarted += HandleLevelStarted;
            GameEvents.PlayerHealthChanged += HandleHealth;
            GameEvents.AbilityStatusChanged += HandleAbility;
            GameEvents.InventoryChanged += RefreshInventory;
            GameEvents.ItemCollected += HandleItemCollected;
            GameEvents.ObjectiveUpdated += HandleObjective;
            GameEvents.InteractionPromptChanged += HandlePrompt;
            GameEvents.WeaponHeatChanged += HandleHeat;
            GameEvents.DamageDealt += HandleDamageDealt;
        }

        public override void Unsubscribe()
        {
            GameEvents.LevelStarted -= HandleLevelStarted;
            GameEvents.PlayerHealthChanged -= HandleHealth;
            GameEvents.AbilityStatusChanged -= HandleAbility;
            GameEvents.InventoryChanged -= RefreshInventory;
            GameEvents.ItemCollected -= HandleItemCollected;
            GameEvents.ObjectiveUpdated -= HandleObjective;
            GameEvents.InteractionPromptChanged -= HandlePrompt;
            GameEvents.WeaponHeatChanged -= HandleHeat;
            GameEvents.DamageDealt -= HandleDamageDealt;
        }

        protected override void OnShow()
        {
            RefreshLevelInfo();
            RefreshInventory();
            touchControls.gameObject.SetActive(UseTouchControls);
        }

        /// <summary>
        /// Touch controls on phones and tablets: native Android / iOS builds (CONDITIONAL
        /// COMPILATION) and WebGL opened in a mobile browser (runtime check). Also true in the
        /// editor's Device Simulator.
        /// </summary>
        public static bool UseTouchControls
        {
            get
            {
#if UNITY_ANDROID || UNITY_IOS
                return true;
#else
                return Application.isMobilePlatform;
#endif
            }
        }

        // ================================================================ build

        private void BuildDamageVignette()
        {
            damageVignette = UIBuilder.Image(Root, "DamageVignette", new Color(1f, 0f, 0f, 0f), IconFactory.Vignette);
            damageVignette.raycastTarget = false;
            UIBuilder.Stretch(damageVignette.rectTransform);
        }

        private void BuildLevelInfo()
        {
            Image panel = UIBuilder.Panel(Root, "LevelInfo", new Color(0f, 0f, 0f, 0.45f));
            UIBuilder.Place(panel.rectTransform, new Vector2(0f, 1f), new Vector2(30f, -30f), new Vector2(420f, 110f));

            levelNumber = UIBuilder.Label(panel.transform, "Number", "LEVEL 1 / 5", 20, UITheme.Accent, TextAnchor.UpperLeft, FontStyle.Bold);
            UIBuilder.Place(levelNumber.rectTransform, new Vector2(0f, 1f), new Vector2(20f, -14f), new Vector2(380f, 28f));
            levelName = UIBuilder.Label(panel.transform, "Name", "", 32, UITheme.Text, TextAnchor.UpperLeft, FontStyle.Bold);
            UIBuilder.Place(levelName.rectTransform, new Vector2(0f, 1f), new Vector2(20f, -40f), new Vector2(300f, 44f));
            levelTimer = UIBuilder.Label(panel.transform, "Timer", "0:00", 26, UITheme.TextMuted, TextAnchor.MiddleRight);
            UIBuilder.Place(levelTimer.rectTransform, new Vector2(1f, 0f), new Vector2(-20f, 14f), new Vector2(120f, 40f));
        }

        private void BuildObjective()
        {
            objectivePanel = UIBuilder.Panel(Root, "Objective", new Color(0f, 0f, 0f, 0.45f));
            UIBuilder.Place(objectivePanel.rectTransform, new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(520f, 110f));

            Text header = UIBuilder.Label(objectivePanel.transform, "Header", "OBJECTIVE", 20, UITheme.Accent, TextAnchor.UpperLeft, FontStyle.Bold);
            UIBuilder.Place(header.rectTransform, new Vector2(0f, 1f), new Vector2(20f, -14f), new Vector2(300f, 28f));
            objectiveCount = UIBuilder.Label(objectivePanel.transform, "Count", "", 22, UITheme.Text, TextAnchor.UpperRight, FontStyle.Bold);
            UIBuilder.Place(objectiveCount.rectTransform, new Vector2(1f, 1f), new Vector2(-20f, -14f), new Vector2(160f, 28f));
            objectiveText = UIBuilder.Label(objectivePanel.transform, "Text", "", 26, UITheme.Text, TextAnchor.UpperLeft);
            UIBuilder.Place(objectiveText.rectTransform, new Vector2(0f, 1f), new Vector2(20f, -42f), new Vector2(480f, 36f));

            Image bar = UIBuilder.Bar(objectivePanel.transform, "Progress", new Color(1f, 1f, 1f, 0.1f), UITheme.Accent, out objectiveFill);
            UIBuilder.Place(bar.rectTransform, new Vector2(0f, 0f), new Vector2(20f, 14f), new Vector2(480f, 10f));
            objectiveFill.fillAmount = 0f;
        }

        private void BuildHealth()
        {
            Image panel = UIBuilder.Panel(Root, "Health", new Color(0f, 0f, 0f, 0.45f));
            UIBuilder.Place(panel.rectTransform, new Vector2(0f, 0f), new Vector2(30f, 30f), new Vector2(480f, 76f));

            heartIcon = UIBuilder.Icon(panel.transform, "Heart", IconFactory.Heart, UITheme.Danger, 44f);
            UIBuilder.Place(heartIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(44f, 44f));

            Image bar = UIBuilder.Bar(panel.transform, "Bar", new Color(1f, 1f, 1f, 0.08f), UITheme.Health, out healthFill);
            UIBuilder.Place(bar.rectTransform, new Vector2(0f, 0.5f), new Vector2(74f, 0f), new Vector2(270f, 26f));

            healthText = UIBuilder.Label(panel.transform, "Value", "100 / 100", 26, UITheme.Text, TextAnchor.MiddleRight, FontStyle.Bold);
            UIBuilder.Place(healthText.rectTransform, new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(120f, 40f));
        }

        private void BuildAbilities()
        {
            RectTransform row = UIBuilder.Place(UIBuilder.Rect("Abilities", Root), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(240f, 110f));
            UIBuilder.Horizontal(row, 18f, TextAnchor.MiddleCenter);

            for (int i = 0; i < slots.Length; i++)
            {
                var slot = new AbilitySlot();
                slot.Frame = UIBuilder.Panel(row, "Ability_" + i, new Color(0f, 0f, 0f, 0.55f));
                UIBuilder.Size(slot.Frame, 100f, 100f);
                slot.Root = slot.Frame.rectTransform;

                slot.Icon = UIBuilder.Icon(slot.Root, "Icon", IconFactory.Circle, UITheme.Text, 60f);
                UIBuilder.Place(slot.Icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(60f, 60f));

                slot.Cooldown = UIBuilder.Image(slot.Root, "Cooldown", new Color(0f, 0f, 0f, 0.7f));
                slot.Cooldown.type = Image.Type.Filled;
                slot.Cooldown.fillMethod = Image.FillMethod.Radial360;
                slot.Cooldown.fillOrigin = (int)Image.Origin360.Top;
                slot.Cooldown.fillClockwise = false;
                slot.Cooldown.fillAmount = 0f;
                UIBuilder.Stretch(slot.Cooldown.rectTransform, 4f);

                slot.Timer = UIBuilder.Label(slot.Root, "Timer", "", 30, UITheme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIBuilder.Stretch(slot.Timer.rectTransform);

                slot.Lock = UIBuilder.Icon(slot.Root, "Lock", IconFactory.Lock, UITheme.Locked, 48f);
                UIBuilder.Place(slot.Lock.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(48f, 48f));

                slot.Key = UIBuilder.Label(slot.Root, "Key", KeyLabel(i), 20, UITheme.Accent, TextAnchor.LowerRight, FontStyle.Bold);
                UIBuilder.Place(slot.Key.rectTransform, new Vector2(1f, 0f), new Vector2(-8f, 4f), new Vector2(60f, 28f));
                slot.Key.text = KeyLabel(i);   // always set: depends on the platform

                slot.FrameColour = slot.Frame.color;
                slot.KeyColour = slot.Key.color;
                SetSlotUnlocked(slot, false);
                slots[i] = slot;
            }
        }

        private void BuildInventory()
        {
            inventoryRow = UIBuilder.Place(UIBuilder.Rect("Inventory", Root), new Vector2(1f, 0f), new Vector2(-30f, 30f), new Vector2(700f, 60f));
            UIBuilder.Horizontal(inventoryRow, 10f, TextAnchor.LowerRight);
        }

        private void BuildCrosshair()
        {
            RectTransform centre = UIBuilder.Place(UIBuilder.Rect("Crosshair", Root), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60f, 60f));
            crosshair = new Image[5];
            Vector2[] offsets = { new Vector2(0, 14), new Vector2(0, -14), new Vector2(14, 0), new Vector2(-14, 0) };
            string[] names = { "Tick_Top", "Tick_Bottom", "Tick_Right", "Tick_Left" };
            for (int i = 0; i < 4; i++)
            {
                crosshair[i] = UIBuilder.Image(centre, names[i], new Color(1f, 1f, 1f, 0.85f));
                bool vertical = i < 2;
                UIBuilder.Place(crosshair[i].rectTransform, new Vector2(0.5f, 0.5f), offsets[i], vertical ? new Vector2(3f, 10f) : new Vector2(10f, 3f));
            }
            crosshair[4] = UIBuilder.Icon(centre, "Dot", IconFactory.Circle, new Color(1f, 1f, 1f, 0.9f), 5f);
            UIBuilder.Place(crosshair[4].rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(5f, 5f));

            // Hit marker: a ring that flashes when a shot lands.
            hitMarker = UIBuilder.Image(centre, "HitMarker", new Color(1f, 1f, 1f, 0f), IconFactory.Ring);
            UIBuilder.Place(hitMarker.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(44f, 44f));

            heatBar = UIBuilder.Bar(centre, "Heat", new Color(0f, 0f, 0f, 0.5f), UITheme.Warning, out heatFill);
            UIBuilder.Place(heatBar.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -44f), new Vector2(120f, 10f));
            heatBar.gameObject.SetActive(false);
        }

        private void BuildPrompt()
        {
            promptPanel = UIBuilder.Panel(Root, "Prompt", new Color(0f, 0f, 0f, 0.6f));
            UIBuilder.Place(promptPanel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -140f), new Vector2(460f, 56f));
            promptText = UIBuilder.Label(promptPanel.transform, "Text", "", 26, UITheme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIBuilder.Stretch(promptText.rectTransform, 6f);
            promptPanel.gameObject.SetActive(false);
        }

        /// <summary>
        /// On-screen controls built from the Input System's OnScreenStick / OnScreenButton. Each one
        /// drives a VIRTUAL GAMEPAD control (left stick = Move, right trigger = Attack...), so the
        /// player, weapons and abilities need no touch-specific code: they already read the
        /// gamepad bindings in InputSystem_Actions.
        /// </summary>
        private void BuildTouchControls()
        {
            touchControls = UIBuilder.Rect("TouchControls", Root);
            bool isNew = UIBuilder.IsNew(touchControls);
            // Inactive BEFORE adding on-screen controls: they register their device in OnEnable
            // and need their control path set first.
            touchControls.gameObject.SetActive(false);
            if (isNew)
            {
                UIBuilder.Stretch(touchControls);
                touchControls.SetSiblingIndex(1);   // above the damage vignette, below the HUD panels
            }

            // Look: drag anywhere on the right half of the screen.
            Image lookArea = UIBuilder.Image(touchControls, "LookArea", new Color(1f, 1f, 1f, 0f));
            if (UIBuilder.IsNew(lookArea))
            {
                lookArea.rectTransform.anchorMin = new Vector2(0.45f, 0f);
                lookArea.rectTransform.anchorMax = new Vector2(1f, 0.85f);
                lookArea.rectTransform.offsetMin = Vector2.zero;
                lookArea.rectTransform.offsetMax = Vector2.zero;
            }
            lookArea.raycastTarget = true;
            UIBuilder.GetOrAdd<OnScreenLookPad>(lookArea.gameObject);

            // Move: virtual joystick, bottom left.
            Image stickBase = UIBuilder.Image(touchControls, "MoveStick", new Color(1f, 1f, 1f, 0.12f), IconFactory.Circle);
            UIBuilder.Place(stickBase.rectTransform, new Vector2(0f, 0f), new Vector2(110f, 140f), new Vector2(260f, 260f));
            Image knob = UIBuilder.Image(stickBase.transform, "Knob", new Color(1f, 1f, 1f, 0.45f), IconFactory.Circle);
            UIBuilder.Place(knob.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110f, 110f));
            knob.raycastTarget = true;
            OnScreenStick stick = UIBuilder.GetOrAdd<OnScreenStick>(knob.gameObject);
            stick.controlPath = "<Gamepad>/leftStick";
            if (UIBuilder.IsNew(knob)) stick.movementRange = 90f;

            // Action buttons, bottom right.
            TouchButton("Fire", "FIRE", null, "<Gamepad>/rightTrigger", new Vector2(-150f, 200f), 170f);
            TouchButton("Bash", "BASH", null, "<Gamepad>/rightShoulder", new Vector2(-360f, 150f), 110f);
            TouchButton("Jump", "JUMP", null, "<Gamepad>/buttonSouth", new Vector2(-170f, 420f), 110f);
            TouchButton("Use", "USE", null, "<Gamepad>/buttonNorth", new Vector2(-370f, 330f), 110f);
            TouchButton("Ability1", "", IconFactory.Dash, "<Gamepad>/leftShoulder", new Vector2(-560f, 150f), 100f);
            TouchButton("Ability2", "", IconFactory.Shield, "<Gamepad>/leftTrigger", new Vector2(-560f, 290f), 100f);

            // Pause, top centre.
            Image pause = TouchButton("Pause", "II", null, "<Gamepad>/start", Vector2.zero, 80f);
            UIBuilder.Place(pause.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(80f, 80f));
        }

        private Image TouchButton(string name, string label, Sprite icon, string controlPath, Vector2 position, float size)
        {
            Image button = UIBuilder.Image(touchControls, "Touch_" + name, new Color(1f, 1f, 1f, 0.16f), IconFactory.Circle);
            UIBuilder.Place(button.rectTransform, new Vector2(1f, 0f), position, new Vector2(size, size));
            button.raycastTarget = true;

            if (icon != null)
            {
                Image image = UIBuilder.Icon(button.transform, "Icon", icon, new Color(1f, 1f, 1f, 0.85f), size * 0.5f);
                UIBuilder.Place(image.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.5f, size * 0.5f));
            }
            else
            {
                Text text = UIBuilder.Label(button.transform, "Label", label, Mathf.RoundToInt(size * 0.2f), new Color(1f, 1f, 1f, 0.9f), TextAnchor.MiddleCenter, FontStyle.Bold);
                UIBuilder.Stretch(text.rectTransform);
            }

            OnScreenButton onScreen = UIBuilder.GetOrAdd<OnScreenButton>(button.gameObject);
            onScreen.controlPath = controlPath;
            return button;
        }

        // ================================================================ events

        private void HandleLevelStarted(int levelIndex)
        {
            RefreshLevelInfo();
            HandlePrompt(null);
            heat = 0f;
            overheated = false;
            damageFlash = 0f;
            lastHealth = -1;
            RefreshInventory();
        }

        private void RefreshLevelInfo()
        {
            if (!LevelManager.HasInstance) return;
            LevelDefinition level = LevelManager.Instance.CurrentLevel;
            int index = LevelManager.Instance.CurrentLevelIndex;
            levelNumber.text = $"LEVEL {index + 1} / {LevelManager.Instance.LevelCount}";
            levelName.text = level != null ? level.DisplayName : "";
        }

        private void HandleHealth(int current, int max)
        {
            healthFraction = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
            healthFill.fillAmount = healthFraction;
            healthFill.color = healthFraction > 0.5f ? healthColour : healthFraction > 0.25f ? UITheme.Warning : UITheme.Danger;
            healthText.text = $"{current} / {max}";

            if (lastHealth >= 0 && current < lastHealth) damageFlash = 0.6f;
            lastHealth = current;
        }

        private void HandleAbility(AbilityStatus status)
        {
            if (status.Slot < 0 || status.Slot >= slots.Length) return;
            AbilitySlot slot = slots[status.Slot];

            slot.Icon.sprite = status.Icon != null ? status.Icon : IconFactory.ForId(status.AbilityId);
            SetSlotUnlocked(slot, status.Unlocked);
            slot.Active = status.Active;
            slot.CooldownLength = Mathf.Max(0.01f, status.CooldownSeconds);
            slot.CooldownEnd = Time.time + status.CooldownRemaining;   // game time: cooldowns pause with the game
        }

        private static void SetSlotUnlocked(AbilitySlot slot, bool unlocked)
        {
            slot.Unlocked = unlocked;
            slot.Icon.gameObject.SetActive(unlocked);
            slot.Lock.gameObject.SetActive(!unlocked);
            Color key = slot.KeyColour;
            if (!unlocked) key.a *= 0.35f;
            slot.Key.color = key;
            if (!unlocked)
            {
                slot.Cooldown.fillAmount = 0f;
                slot.Timer.text = "";
            }
        }

        private void HandleItemCollected(ItemCollectedInfo info) => RefreshInventory();

        private void RefreshInventory()
        {
            foreach (GameObject chip in inventoryChips) Object.Destroy(chip);
            inventoryChips.Clear();

            PlayerInventory inventory = Object.FindAnyObjectByType<PlayerInventory>();
            if (inventory == null) return;

            foreach (KeyValuePair<string, int> item in inventory.Items)
            {
                if (item.Value <= 0) continue;
                inventoryChips.Add(CreateChip(item.Key, inventory.DisplayNameOf(item.Key), item.Value));
            }
        }

        private GameObject CreateChip(string itemId, string displayName, int amount)
        {
            Image chip = UIBuilder.Panel(inventoryRow, "Item_" + itemId, new Color(0f, 0f, 0f, 0.55f));
            UIBuilder.Horizontal(chip, 8f, TextAnchor.MiddleCenter, 10);
            UIBuilder.Size(UIBuilder.Icon(chip.transform, "Icon", IconFactory.ForId(itemId), ItemColour(itemId), 34f), 34f, 34f);
            string text = amount > 1 ? $"{displayName}  x{amount}" : displayName;
            Text label = UIBuilder.Label(chip.transform, "Name", text, 22, UITheme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIBuilder.Size(label, -1f, 40f);
            return chip.gameObject;
        }

        private static Color ItemColour(string itemId)
        {
            string id = itemId.ToLowerInvariant();
            if (id.Contains("red")) return UITheme.Danger;
            if (id.Contains("blue")) return new Color(0.35f, 0.55f, 1f);
            if (id.Contains("green")) return UITheme.Health;
            if (id.Contains("credit") || id.Contains("gold")) return UITheme.Gold;
            return UITheme.Accent;
        }

        private void HandleObjective(string description, int current, int target)
        {
            objectiveText.text = description;
            objectiveCount.text = target > 1 ? $"{current} / {target}" : "";
            objectiveFill.fillAmount = target > 0 ? Mathf.Clamp01((float)current / target) : 0f;
            if (target > 0 && current >= target) objectiveFlash = 1f;
        }

        private void HandlePrompt(string prompt)
        {
            bool show = !string.IsNullOrEmpty(prompt);
            promptPanel.gameObject.SetActive(show);
            if (show) promptText.text = $"[{InteractKey}]  {prompt}";
        }

        private void HandleHeat(float value, bool isOverheated)
        {
            heat = value;
            overheated = isOverheated;
        }

        private void HandleDamageDealt(DamageDealtInfo info)
        {
            hitFlash = 1f;
            hitMarker.color = info.Killed ? UITheme.Danger : info.Blocked ? UITheme.Locked : Color.white;
        }

        // ================================================================ per frame

        protected override void OnTick(float dt)
        {
            if (LevelManager.HasInstance && LevelManager.Instance.CurrentLevelIndex >= 0)
                levelTimer.text = UIBuilder.FormatTime(LevelManager.Instance.CurrentLevelElapsed);

            // Damage vignette fades out; below 25% health it pulses.
            damageFlash = Mathf.Max(0f, damageFlash - dt);
            float lowHealth = healthFraction > 0f && healthFraction <= 0.25f ? 0.25f + 0.15f * Mathf.Sin(Time.unscaledTime * 6f) : 0f;
            damageVignette.color = new Color(1f, 0f, 0f, Mathf.Max(damageFlash, lowHealth));
            heartIcon.rectTransform.localScale = Vector3.one * (1f + (lowHealth > 0f ? 0.1f * Mathf.Sin(Time.unscaledTime * 8f) : 0f));

            // Abilities: cooldown sweep + seconds left, glow while active.
            foreach (AbilitySlot slot in slots)
            {
                if (!slot.Unlocked) continue;
                float remaining = Mathf.Max(0f, slot.CooldownEnd - Time.time);
                slot.Cooldown.fillAmount = remaining / slot.CooldownLength;
                slot.Timer.text = remaining > 0.05f ? Mathf.CeilToInt(remaining).ToString() : "";
                slot.Frame.color = slot.Active ? Color.Lerp(slot.FrameColour, UITheme.Accent, 0.5f) : slot.FrameColour;
            }

            // Objective panel flashes green when an objective is completed.
            objectiveFlash = Mathf.Max(0f, objectiveFlash - dt * 1.5f);
            objectivePanel.color = Color.Lerp(objectiveColour, new Color(0.15f, 0.6f, 0.3f, 0.7f), objectiveFlash);

            // Hit marker and heat bar.
            hitFlash = Mathf.Max(0f, hitFlash - dt * 5f);
            Color marker = hitMarker.color;
            marker.a = hitFlash;
            hitMarker.color = marker;
            hitMarker.rectTransform.localScale = Vector3.one * (0.6f + 0.4f * hitFlash);

            heatBar.gameObject.SetActive(heat > 0.01f);
            heatFill.fillAmount = heat;
            heatFill.color = overheated ? (Mathf.Sin(Time.unscaledTime * 20f) > 0f ? UITheme.Danger : UITheme.Warning) : Color.Lerp(UITheme.Accent, UITheme.Warning, heat);
            Color tick = overheated ? UITheme.Danger : crosshairColour;
            for (int i = 0; i < crosshair.Length; i++) crosshair[i].color = tick;
        }

        // ================================================================ platform labels

        private static string KeyLabel(int slot)
        {
#if UNITY_ANDROID || UNITY_IOS
            return "";   // mobile uses on-screen buttons
#else
            return slot == 0 ? "Q" : "R";
#endif
        }

        private static string InteractKey
        {
            get
            {
#if UNITY_ANDROID || UNITY_IOS
                return "Tap";
#else
                return "E";
#endif
            }
        }
    }
}
