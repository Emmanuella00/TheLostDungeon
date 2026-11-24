using TheLostDungeon.Events;
using TheLostDungeon.Items;
using TheLostDungeon.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TheLostDungeon.Interaction
{
    /// <summary>
    /// A floating status sign above a door, telling the player what this door needs to open:
    ///
    ///   locked by switches  -> red padlock   "Switches 1/3"      (counts up live)
    ///   locked by a keycard -> red key       "Needs Red Keycard" -> amber "Use Red Keycard" once held
    ///   opened by the level -> red padlock   the current objective
    ///   open / openable     -> green unlock  "EXIT" (gentle pulse)
    ///
    /// It works the requirement out from the Door itself (its Required Key), any SwitchGate that
    /// targets the door, and the current objective (GameEvents.ObjectiveUpdated), so all you do is
    /// add this component to the door. The sign faces the camera, fades with distance and lights
    /// the area in its colour.
    /// </summary>
    [RequireComponent(typeof(Door))]
    public class ExitSign : MonoBehaviour
    {
        private enum SignState { Open, Switches, NeedsKey, HasKey, Objective }

        [SerializeField] private string label = "EXIT";
        [Tooltip("Extra height above the top of the door.")]
        [SerializeField] private float heightAboveDoor = 0.5f;
        [Tooltip("Moves the sign relative to the door: X = sideways, Y = up, Z = forward (+) / backward (-). " +
                 "Uses the door's own axes, so the blue Z arrow of the door is 'forward'. Works live in Play mode.")]
        [SerializeField] private Vector3 offset = Vector3.zero;
        [Tooltip("On: the sign always turns to face the player. Off: it is fixed to the door like a real sign " +
                 "(door rotation + Rotation Offset).")]
        [SerializeField] private bool faceCamera = true;
        [Tooltip("Degrees. Used when Face Camera is off (and as a small tilt when it is on). " +
                 "Y = 180 flips the sign to the other side of the door.")]
        [SerializeField] private Vector3 rotationOffset = Vector3.zero;
        [SerializeField] private float visibleDistance = 30f;
        [SerializeField] private bool addLight = true;

        private static readonly Color Red = new Color(1f, 0.3f, 0.25f);
        private static readonly Color Amber = new Color(1f, 0.7f, 0.2f);
        private static readonly Color Green = new Color(0.3f, 1f, 0.5f);

        private Door door;
        private SwitchGate gate;
        private PlayerInventory inventory;
        private string currentObjective = "";

        private Transform sign;
        private CanvasGroup group;
        private Image panel, icon;
        private Text text;
        private Light glow;
        private SignState state = (SignState)(-1);
        private Vector3 basePosition;
        private float nextRefresh;

        private void Awake() => door = GetComponent<Door>();

        private void OnEnable()
        {
            GameEvents.ObjectiveUpdated += HandleObjective;
            if (sign != null) sign.gameObject.SetActive(true);
        }

        private void OnDisable()
        {
            GameEvents.ObjectiveUpdated -= HandleObjective;
            if (sign != null) sign.gameObject.SetActive(false);
        }

        private void Start()
        {
            foreach (SwitchGate candidate in FindObjectsByType<SwitchGate>(FindObjectsSortMode.None))
            {
                if (candidate.Controls(door)) { gate = candidate; break; }
            }
            BuildSign();
            Refresh();
        }

        private void HandleObjective(string description, int current, int target)
        {
            currentObjective = target > 1 ? $"{description}  {current}/{target}" : description;
            Refresh();
        }

        // ------------------------------------------------------------------ per frame

        private void LateUpdate()
        {
            Camera cam = Camera.main;
            if (cam == null || sign == null) return;

            sign.position = SignPosition();   // live: tweak Offset in Play mode and see it move

            sign.rotation = SignRotation(cam);

            // Fade out with distance so far-away signs don't clutter the view.
            float distance = Vector3.Distance(cam.transform.position, sign.position);
            group.alpha = Mathf.Clamp01((visibleDistance + 8f - distance) / 8f);

            // Gentle pulse once the way is open.
            float pulse = state == SignState.Open ? 1f + 0.06f * Mathf.Sin(Time.time * 4f) : 1f;
            sign.localScale = Vector3.one * 0.01f * pulse;

            if (Time.time >= nextRefresh) Refresh();   // cheap safety net: door, keys, switches
        }

        private void Refresh()
        {
            if (sign == null) return;
            nextRefresh = Time.time + 0.25f;
            if (inventory == null) inventory = FindAnyObjectByType<PlayerInventory>();

            ItemDefinition key = door.RequiredKey;
            SignState next;
            string message;

            if (door.IsOpen || (gate != null && gate.IsOpen))
            {
                next = SignState.Open; message = label;
            }
            else if (gate != null)
            {
                next = SignState.Switches; message = $"Switches {gate.OnCount}/{gate.RequiredCount}";
            }
            else if (key != null)
            {
                bool hasKey = inventory != null && inventory.Has(key);
                next = hasKey ? SignState.HasKey : SignState.NeedsKey;
                message = (hasKey ? "Use " : " ") + key.DisplayName;
            }
            else if (!door.PlayerCanOpen)
            {
                next = SignState.Objective;
                message = string.IsNullOrEmpty(currentObjective) ? "Locked" : currentObjective;
            }
            else
            {
                next = SignState.Open; message = label;
            }

            text.text = message;
            if (next == state) return;
            state = next;

            Color colour = next == SignState.Open ? Green : next == SignState.HasKey ? Amber : Red;
            icon.sprite = next == SignState.Open ? IconFactory.Unlock
                        : next == SignState.NeedsKey || next == SignState.HasKey ? IconFactory.Key
                        : IconFactory.Lock;
            icon.color = colour;
            text.color = colour;
            panel.color = new Color(colour.r * 0.12f, colour.g * 0.12f, colour.b * 0.12f, 0.85f);
            if (glow != null) glow.color = colour;
        }

        /// <summary>Centre-top of the door's meshes (measured while closed).</summary>
        private Vector3 MeasureTopOfDoor()
        {
            Bounds bounds = new Bounds(transform.position, Vector3.zero);
            foreach (Renderer r in GetComponentsInChildren<Renderer>()) bounds.Encapsulate(r.bounds);
            return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
        }

        private Vector3 SignPosition() =>
            basePosition + Vector3.up * heightAboveDoor + transform.TransformDirection(offset);

        /// <summary>
        /// Face Camera on: billboard toward the camera. Off: fixed to the door.
        /// A world-space canvas is readable from behind its forward axis, so "fixed" uses the
        /// door's rotation turned 180° (text faces out of the door's front) plus the offset.
        /// </summary>
        private Quaternion SignRotation(Camera cam)
        {
            if (faceCamera && cam != null)
            {
                Vector3 awayFromCamera = SignPosition() - cam.transform.position;
                return Quaternion.LookRotation(awayFromCamera) * Quaternion.Euler(rotationOffset);
            }
            return transform.rotation * Quaternion.Euler(0f, 180f, 0f) * Quaternion.Euler(rotationOffset);
        }

#if UNITY_EDITOR
        /// <summary>Preview in the Scene view (select the door): yellow box = where the sign will be.</summary>
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) basePosition = MeasureTopOfDoor();
            Vector3 position = SignPosition();
            Quaternion rotation = SignRotation(faceCamera ? Camera.current : null);

            // Draw the box rotated like the real sign; the line shows which way the text faces.
            Gizmos.color = Color.yellow;
            Gizmos.matrix = Matrix4x4.TRS(position, rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(3.2f, 0.9f, 0.05f));
            Gizmos.DrawLine(Vector3.zero, Vector3.back * 0.8f);   // text is read from this side
            Gizmos.matrix = Matrix4x4.identity;
        }
#endif

        // ------------------------------------------------------------------ build

        private void BuildSign()
        {
            basePosition = MeasureTopOfDoor();

            // Not parented to the door: a scaled door would stretch the sign. It still lives in this
            // level's scene, so it is unloaded with the level.
            var root = new GameObject(name + "_ExitSign", typeof(RectTransform));
            sign = root.transform;
            sign.position = SignPosition();
            sign.localScale = Vector3.one * 0.01f;   // 1 UI pixel = 1 cm

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            root.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 10f;   // sharp text up close
            ((RectTransform)sign).sizeDelta = new Vector2(320f, 90f);
            group = root.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;

            panel = UIBuilder.Panel(sign, "Panel", new Color(0f, 0f, 0f, 0.85f));
            UIBuilder.Stretch(panel.rectTransform);
            UIBuilder.Horizontal(panel, 12f, TextAnchor.MiddleCenter, 12);

            icon = UIBuilder.Size(UIBuilder.Icon(panel.transform, "Icon", IconFactory.Lock, Red, 56f), 56f, 56f);
            text = UIBuilder.Label(panel.transform, "Text", label, 34, Red, TextAnchor.MiddleLeft, FontStyle.Bold);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIBuilder.Size(text, 220f, 70f);

            if (addLight)
            {
                var lightObject = new GameObject("SignLight");
                lightObject.transform.SetParent(sign, false);
                lightObject.transform.localPosition = new Vector3(0f, 0f, -60f);   // 60 cm in front
                glow = lightObject.AddComponent<Light>();
                glow.type = LightType.Point;
                glow.range = 5f;
                glow.intensity = 1.5f;
                glow.shadows = LightShadows.None;
            }
        }
    }
}